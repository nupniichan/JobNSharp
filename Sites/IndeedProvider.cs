using System.Text;
using System.Text.Json;
using JobNSharp.Interfaces;
using JobNSharp.Models;
using JobNSharp.Models.Enums;
using JobNSharp.Utils;
using JobNSharp.Utils.Exceptions;

namespace JobNSharp.Sites;

public class IndeedProvider : IJobProvider
{
    private const string ApiUrl = "https://apis.indeed.com/graphql";
    private const int PageSize = 100;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRateLimiter _rateLimiter;

    public ESite SiteName => ESite.Indeed;

    public IndeedProvider(IHttpClientFactory httpClientFactory, IRateLimiter rateLimiter)
    {
        _httpClientFactory = httpClientFactory;
        _rateLimiter = rateLimiter;
    }

    public async Task<List<JobResult>> CrawlAsync(CrawlRequest request, CancellationToken ct = default)
    {
        return await _rateLimiter.ExecuteAsync(SiteName.ToString(), async () =>
        {
            var client = _httpClientFactory.CreateClient("IndeedApi");
            var countryCode = CountryMapper.GetCountryCode(request.Country);
            var domain = CountryMapper.GetDomain(request.Country);
            var results = new List<JobResult>();
            string? cursor = null;

            while (results.Count < request.ResultWanted)
            {
                var (jobs, nextCursor) = await FetchPageAsync(client, request, cursor, countryCode, domain, ct);
                if (jobs.Count == 0) break;

                results.AddRange(jobs);
                cursor = nextCursor;
                if (string.IsNullOrEmpty(cursor)) break;
            }

            return results.Take(request.ResultWanted).ToList();
        }, ct);
    }

    private static async Task<(List<JobResult>, string?)> FetchPageAsync(
        HttpClient client, CrawlRequest request, string? cursor,
        string countryCode, string domain, CancellationToken ct)
    {
        var query = BuildGraphQlQuery(request, cursor);
        var payload = JsonSerializer.Serialize(new { query });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        httpRequest.Headers.Add("indeed-co", countryCode);

        using var response = await client.SendAsync(httpRequest, ct);
        if (!response.IsSuccessStatusCode)
            throw new CrawlException("indeed", $"Indeed API returned {(int)response.StatusCode} ({response.ReasonPhrase})");

        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseResponse(json, domain, request.IncludeDescription);
    }

    private static string BuildGraphQlQuery(CrawlRequest request, string? cursor)
    {
        var searchTerm = request.SearchTerm.Replace("\"", "\\\"");
        var what = $"what: \"{searchTerm}\"";

        var location = "";
        var where = BuildLocationString(request.City, request.Country);
        if (!string.IsNullOrEmpty(where))
            location = $"location: {{where: \"{where}\", radius: 50, radiusUnit: MILES}}";

        var cursorPart = !string.IsNullOrEmpty(cursor) ? $"cursor: \"{cursor}\"" : "";
        var filters = BuildFilters(request);

        return $$"""
        query GetJobData {
            jobSearch(
                {{what}}
                {{location}}
                limit: {{PageSize}}
                {{cursorPart}}
                sort: RELEVANCE
                {{filters}}
            ) {
                pageInfo { nextCursor }
                results {
                    job {
                        key
                        title
                        datePublished
                        description { html }
                        location {
                            countryName
                            countryCode
                            admin1Code
                            city
                        }
                        compensation {
                            baseSalary {
                                unitOfWork
                                range { ... on Range { min max } }
                            }
                            currencyCode
                        }
                        attributes { key label }
                        employer { name }
                        recruit { viewJobUrl }
                    }
                }
            }
        }
        """;
    }

    private static string BuildFilters(CrawlRequest request)
    {
        if (request.DatePosted.HasValue && request.DatePosted != EDatePosted.Anytime)
        {
            var hours = request.DatePosted switch
            {
                EDatePosted.Past24Hours => 24,
                EDatePosted.PastWeek => 168,
                EDatePosted.PastMonth => 720,
                _ => 0
            };
            if (hours > 0)
                return $$"""filters: { date: { field: "dateOnIndeed", start: "{{hours}}h" } }""";
        }

        var keys = new List<string>();

        if (request.JobType is { Count: > 0 })
        {
            foreach (var jt in request.JobType)
            {
                var key = jt switch
                {
                    EJobType.FullTime => "CF3CP",
                    EJobType.PartTime => "75GKK",
                    EJobType.Contract => "NJXCK",
                    EJobType.Internship => "VDTG7",
                    _ => null
                };
                if (key != null) keys.Add(key);
            }
        }

        if (request.Remote == true)
            keys.Add("DSQF7");

        if (keys.Count > 0)
        {
            var keysStr = string.Join("\", \"", keys);
            return "filters: { composite: { filters: [{ keyword: { field: \"attributes\", keys: [\"" + keysStr + "\"] } }] } }";
        }

        return "";
    }

    private static (List<JobResult>, string?) ParseResponse(string json, string domain, bool includeDescription)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data))
            return ([], null);

        var jobSearch = data.GetProperty("jobSearch");
        string? nextCursor = null;
        if (jobSearch.TryGetProperty("pageInfo", out var pageInfo))
            nextCursor = pageInfo.GetProperty("nextCursor").GetString();

        var results = new List<JobResult>();

        foreach (var result in jobSearch.GetProperty("results").EnumerateArray())
        {
            var job = result.GetProperty("job");
            var key = job.GetProperty("key").GetString() ?? "";
            var title = job.GetProperty("title").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(title)) continue;

            var jobResult = new JobResult
            {
                Site = ESite.Indeed,
                Title = title,
                SourceId = key,
                JobUrl = $"https://{domain}.indeed.com/viewjob?jk={key}"
            };

            if (job.TryGetProperty("employer", out var employer) && employer.ValueKind == JsonValueKind.Object)
                jobResult.Company = employer.GetProperty("name").GetString() ?? "";

            if (job.TryGetProperty("location", out var loc) && loc.ValueKind == JsonValueKind.Object)
            {
                jobResult.Location = new LocationModel
                {
                    City = loc.GetProperty("city").GetString(),
                    Country = loc.GetProperty("countryName").GetString()
                };
            }

            if (job.TryGetProperty("datePublished", out var datePub) && datePub.ValueKind == JsonValueKind.Number)
            {
                var ts = datePub.GetInt64() / 1000;
                jobResult.PostedDate = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
            }

            if (job.TryGetProperty("compensation", out var comp) && comp.ValueKind == JsonValueKind.Object)
                ParseCompensation(comp, jobResult);

            if (job.TryGetProperty("attributes", out var attrs) && attrs.ValueKind == JsonValueKind.Array)
            {
                foreach (var attr in attrs.EnumerateArray())
                {
                    var label = attr.GetProperty("label").GetString();
                    var mapped = MapLabelToJobType(label);
                    if (mapped.HasValue) { jobResult.JobType = mapped.Value; break; }
                }
            }

            if (includeDescription && job.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.Object)
                jobResult.Description = desc.GetProperty("html").GetString();

            results.Add(jobResult);
        }

        return (results, nextCursor);
    }

    private static void ParseCompensation(JsonElement comp, JobResult job)
    {
        if (!comp.TryGetProperty("baseSalary", out var salary) || salary.ValueKind != JsonValueKind.Object)
            return;

        if (salary.TryGetProperty("unitOfWork", out var unit))
        {
            job.Interval = unit.GetString()?.ToLower() switch
            {
                "year" => "yearly",
                "month" => "monthly",
                "hour" => "hourly",
                "week" => "weekly",
                "day" => "daily",
                _ => unit.GetString()
            };
        }

        if (salary.TryGetProperty("range", out var range) && range.ValueKind == JsonValueKind.Object)
        {
            if (range.TryGetProperty("min", out var min) && min.ValueKind == JsonValueKind.Number)
                job.MinAmount = min.GetDecimal();
            if (range.TryGetProperty("max", out var max) && max.ValueKind == JsonValueKind.Number)
                job.MaxAmount = max.GetDecimal();
        }
    }

    private static EJobType? MapLabelToJobType(string? label) => label?.ToLower() switch
    {
        "full-time" => EJobType.FullTime,
        "part-time" => EJobType.PartTime,
        "contract" => EJobType.Contract,
        "internship" => EJobType.Internship,
        "temporary" => EJobType.Temporary,
        _ => null
    };

    private static string BuildLocationString(string? city, string? country)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(country)) parts.Add(country);
        return string.Join(", ", parts);
    }

}
