using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobNSharp.Interfaces;
using JobNSharp.Models;
using JobNSharp.Models.Enums;
using JobNSharp.Utils.Exceptions;

namespace JobNSharp.Sites;

public partial class GlassdoorProvider : IJobProvider
{
    private const string BaseUrl = "https://www.glassdoor.com";
    private const int PageSize = 30;
    private const int MaxPages = 30;
    private const string FallbackToken =
        "Ft6oHEWlRZrxDww95Cpazw:0pGUrkb2y3TyOpAIqF2vbPmUXoXVkD3oEGDVkvfeCerceQ5-n8mBg3BovySUIjmCPHCaW0H2nQVdqzbtsYqf4Q:wcqRqeegRUa9MVLJGyujVXB7vWFPjdaS1CtrrzJq-ok";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRateLimiter _rateLimiter;

    public ESite SiteName => ESite.Glassdoor;

    public GlassdoorProvider(IHttpClientFactory httpClientFactory, IRateLimiter rateLimiter)
    {
        _httpClientFactory = httpClientFactory;
        _rateLimiter = rateLimiter;
    }

    public async Task<List<JobResult>> CrawlAsync(CrawlRequest request, CancellationToken ct = default)
    {
        return await _rateLimiter.ExecuteAsync(SiteName.ToString(), async () =>
        {
            var client = _httpClientFactory.CreateClient("GlassdoorApi");

            var csrfToken = await GetCsrfTokenAsync(client, ct) ?? FallbackToken;
            var (locationId, locationType) = await ResolveLocationAsync(client, request, csrfToken, ct);

            var results = new List<JobResult>();
            string? cursor = null;

            for (var page = 1; results.Count < request.ResultWanted && page <= MaxPages; page++)
            {
                var (jobs, nextCursor) = await FetchPageAsync(
                    client, request, locationId, locationType, page, cursor, csrfToken, ct);

                if (jobs.Count == 0) break;
                results.AddRange(jobs);
                cursor = nextCursor;
                if (cursor == null) break;
            }

            return results.Take(request.ResultWanted).ToList();
        }, ct);
    }

    private static async Task<string?> GetCsrfTokenAsync(HttpClient client, CancellationToken ct)
    {
        try
        {
            var response = await client.GetAsync($"{BaseUrl}/Job/computer-science-jobs.htm", ct);
            if (!response.IsSuccessStatusCode) return null;

            var html = await response.Content.ReadAsStringAsync(ct);
            var match = CsrfTokenPattern().Match(html);
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static readonly (int Id, string Type) DefaultLocation = (11047, "STATE");

    private static async Task<(int LocationId, string LocationType)> ResolveLocationAsync(
        HttpClient client, CrawlRequest request, string csrfToken, CancellationToken ct)
    {
        if (request.Remote == true ||
            (string.IsNullOrWhiteSpace(request.City) && string.IsNullOrWhiteSpace(request.Country)))
            return DefaultLocation;

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.City))
            candidates.Add(request.City);
        var combined = BuildLocationString(request.City, request.Country);
        if (combined != candidates.FirstOrDefault())
            candidates.Add(combined);

        foreach (var term in candidates)
        {
            var result = await TryResolveLocationAsync(client, term, csrfToken, ct);
            if (result.HasValue) return result.Value;
        }

        return DefaultLocation;
    }

    private static async Task<(int Id, string Type)?> TryResolveLocationAsync(
        HttpClient client, string term, string csrfToken, CancellationToken ct)
    {
        var url = $"{BaseUrl}/findPopularLocationAjax.htm?maxLocationsToReturn=10&term={Uri.EscapeDataString(term)}";

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
            httpRequest.Headers.Add("gd-csrf-token", csrfToken);

            using var response = await client.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var items = doc.RootElement;
            if (items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0) return null;

            var first = items[0];

            var locIdEl = first.GetProperty("locationId");
            var locId = locIdEl.ValueKind == JsonValueKind.String
                ? int.Parse(locIdEl.GetString()!)
                : locIdEl.GetInt32();

            var locType = first.GetProperty("locationType").GetString() switch
            {
                "C" => "CITY",
                "S" => "STATE",
                "N" => "COUNTRY",
                var t => t ?? "CITY"
            };

            return (locId, locType);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<(List<JobResult>, string?)> FetchPageAsync(
        HttpClient client, CrawlRequest request, int locationId, string locationType,
        int page, string? cursor, string csrfToken, CancellationToken ct)
    {
        var payload = BuildPayload(request, locationId, locationType, page, cursor);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/graph");
        httpRequest.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        httpRequest.Headers.Add("gd-csrf-token", csrfToken);

        using var response = await client.SendAsync(httpRequest, ct);
        if (!response.IsSuccessStatusCode)
            throw new CrawlException("glassdoor",
                $"Glassdoor API returned {(int)response.StatusCode} ({response.ReasonPhrase})");

        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseResponse(json, page, request.IncludeDescription);
    }

    private static string BuildPayload(
        CrawlRequest request, int locationId, string locationType, int page, string? cursor)
    {
        var filterParams = new List<object>();

        if (request.DatePosted.HasValue && request.DatePosted != EDatePosted.Anytime)
        {
            var days = request.DatePosted switch
            {
                EDatePosted.Past24Hours => 1,
                EDatePosted.PastWeek => 7,
                EDatePosted.PastMonth => 30,
                _ => 0
            };
            if (days > 0)
                filterParams.Add(new { filterKey = "fromAge", values = days.ToString() });
        }

        if (request.JobType is { Count: > 0 })
        {
            var gdType = request.JobType[0] switch
            {
                EJobType.FullTime => "fulltime",
                EJobType.PartTime => "parttime",
                EJobType.Contract => "contract",
                EJobType.Internship => "internship",
                EJobType.Temporary => "temporary",
                _ => (string?)null
            };
            if (gdType != null)
                filterParams.Add(new { filterKey = "jobType", values = gdType });
        }

        var variables = new Dictionary<string, object?>
        {
            ["excludeJobListingIds"] = Array.Empty<long>(),
            ["filterParams"] = filterParams,
            ["keyword"] = request.SearchTerm,
            ["numJobsToShow"] = PageSize,
            ["locationType"] = locationType,
            ["locationId"] = locationId,
            ["parameterUrlInput"] = $"IL.0,12_I{locationType}{locationId}",
            ["pageNumber"] = page,
            ["pageCursor"] = cursor
        };

        var payloadArray = new object[]
        {
            new
            {
                operationName = "JobSearchResultsQuery",
                variables,
                query = QueryTemplate
            }
        };

        return JsonSerializer.Serialize(payloadArray);
    }

    private static (List<JobResult>, string?) ParseResponse(string json, int currentPage, bool includeDescription)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var responseObj = root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0
            ? root[0]
            : root;

        if (!responseObj.TryGetProperty("data", out var data))
            return ([], null);

        var jobListings = data.GetProperty("jobListings");

        string? nextCursor = null;
        if (jobListings.TryGetProperty("paginationCursors", out var cursors))
        {
            foreach (var c in cursors.EnumerateArray())
            {
                if (c.GetProperty("pageNumber").GetInt32() == currentPage + 1)
                {
                    nextCursor = c.GetProperty("cursor").GetString();
                    break;
                }
            }
        }

        var results = new List<JobResult>();
        if (!jobListings.TryGetProperty("jobListings", out var jobs))
            return (results, nextCursor);

        foreach (var listing in jobs.EnumerateArray())
        {
            if (!listing.TryGetProperty("jobview", out var jobview))
                continue;

            var header = jobview.GetProperty("header");
            var job = jobview.GetProperty("job");

            var listingIdEl = job.GetProperty("listingId");
            var listingId = listingIdEl.ValueKind == JsonValueKind.String
                ? listingIdEl.GetString()!
                : listingIdEl.GetInt64().ToString();

            var title = header.GetProperty("jobTitleText").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(title)) continue;

            var company = header.TryGetProperty("employerNameFromSearch", out var empName)
                ? empName.GetString() ?? ""
                : "";

            var jobResult = new JobResult
            {
                Site = ESite.Glassdoor,
                Title = title,
                Company = company,
                SourceId = $"gd-{listingId}",
                JobUrl = $"{BaseUrl}/job-listing/j?jl={listingId}"
            };

            ParseLocation(header, jobResult);
            ParsePostedDate(header, jobResult);
            ParseCompensation(header, jobResult);

            if (includeDescription &&
                job.TryGetProperty("description", out var desc) &&
                desc.ValueKind == JsonValueKind.String)
            {
                jobResult.Description = desc.GetString();
            }

            results.Add(jobResult);
        }

        return (results, nextCursor);
    }

    private static void ParseLocation(JsonElement header, JobResult job)
    {
        var locationName = header.TryGetProperty("locationName", out var locName)
            ? locName.GetString()
            : null;
        var locationType = header.TryGetProperty("locationType", out var locType)
            ? locType.GetString()
            : null;

        if (locationType == "S" || string.IsNullOrWhiteSpace(locationName))
            return;

        var parts = locationName.Split(',', 2);
        job.Location = new LocationModel
        {
            City = parts[0].Trim(),
            Country = parts.Length > 1 ? parts[1].Trim() : null
        };
    }

    private static void ParsePostedDate(JsonElement header, JobResult job)
    {
        if (header.TryGetProperty("ageInDays", out var age) && age.ValueKind == JsonValueKind.Number)
            job.PostedDate = DateTime.UtcNow.AddDays(-age.GetInt32()).Date;
    }

    private static void ParseCompensation(JsonElement header, JobResult job)
    {
        if (!header.TryGetProperty("payPeriod", out var payPeriod) ||
            payPeriod.ValueKind != JsonValueKind.String)
            return;

        if (!header.TryGetProperty("payPeriodAdjustedPay", out var adjustedPay) ||
            adjustedPay.ValueKind != JsonValueKind.Object)
            return;

        job.Interval = payPeriod.GetString()?.ToUpper() switch
        {
            "ANNUAL" => "yearly",
            "MONTHLY" => "monthly",
            "WEEKLY" => "weekly",
            "DAILY" => "daily",
            "HOURLY" => "hourly",
            _ => payPeriod.GetString()?.ToLower()
        };

        if (adjustedPay.TryGetProperty("p10", out var p10) && p10.ValueKind == JsonValueKind.Number)
            job.MinAmount = p10.GetDecimal();
        if (adjustedPay.TryGetProperty("p90", out var p90) && p90.ValueKind == JsonValueKind.Number)
            job.MaxAmount = p90.GetDecimal();
    }

    private static string BuildLocationString(string? city, string? country)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(country)) parts.Add(country);
        return string.Join(", ", parts);
    }

    [GeneratedRegex(@"""token"":\s*""([^""]+)""")]
    private static partial Regex CsrfTokenPattern();

    private const string QueryTemplate = """
        query JobSearchResultsQuery(
            $excludeJobListingIds: [Long!],
            $keyword: String,
            $locationId: Int,
            $locationType: LocationTypeEnum,
            $numJobsToShow: Int!,
            $pageCursor: String,
            $pageNumber: Int,
            $filterParams: [FilterParams],
            $originalPageUrl: String,
            $seoFriendlyUrlInput: String,
            $parameterUrlInput: String,
            $seoUrl: Boolean
        ) {
            jobListings(
                contextHolder: {
                    searchParams: {
                        excludeJobListingIds: $excludeJobListingIds,
                        keyword: $keyword,
                        locationId: $locationId,
                        locationType: $locationType,
                        numPerPage: $numJobsToShow,
                        pageCursor: $pageCursor,
                        pageNumber: $pageNumber,
                        filterParams: $filterParams,
                        originalPageUrl: $originalPageUrl,
                        seoFriendlyUrlInput: $seoFriendlyUrlInput,
                        parameterUrlInput: $parameterUrlInput,
                        seoUrl: $seoUrl,
                        searchType: SR
                    }
                }
            ) {
                jobListings {
                    ...JobView
                    __typename
                }
                paginationCursors {
                    cursor
                    pageNumber
                    __typename
                }
                totalJobsCount
                __typename
            }
        }

        fragment JobView on JobListingSearchResult {
            jobview {
                header {
                    adOrderSponsorshipLevel
                    ageInDays
                    easyApply
                    employer {
                        id
                        name
                        shortName
                        __typename
                    }
                    employerNameFromSearch
                    jobCountryId
                    jobLink
                    jobTitleText
                    locationName
                    locationType
                    locId
                    payCurrency
                    payPeriod
                    payPeriodAdjustedPay {
                        p10
                        p50
                        p90
                        __typename
                    }
                    sponsored
                    __typename
                }
                job {
                    description
                    jobTitleId
                    jobTitleText
                    listingId
                    __typename
                }
                overview {
                    shortName
                    squareLogoUrl
                    __typename
                }
                __typename
            }
            __typename
        }
        """;
}
