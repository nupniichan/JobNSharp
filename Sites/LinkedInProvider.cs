using System.Net;
using System.Text.RegularExpressions;
using JobNSharp.Interfaces;
using JobNSharp.Models;
using JobNSharp.Models.Enums;
using JobNSharp.Utils;
using JobNSharp.Utils.Exceptions;

namespace JobNSharp.Sites;

public partial class LinkedInProvider : IJobProvider
{
    private const string BaseUrl = "https://www.linkedin.com/jobs-guest/jobs/api/seeMoreJobPostings/search";
    private const int PageSize = 25;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRateLimiter _rateLimiter;

    public ESite SiteName => ESite.LinkedIn;

    public LinkedInProvider(IHttpClientFactory httpClientFactory, IRateLimiter rateLimiter)
    {
        _httpClientFactory = httpClientFactory;
        _rateLimiter = rateLimiter;
    }

    public async Task<List<JobResult>> CrawlAsync(CrawlRequest request, CancellationToken ct = default)
    {
        return await _rateLimiter.ExecuteAsync(SiteName.ToString(), async () =>
        {
            var client = _httpClientFactory.CreateClient("JobCrawler");
            var results = new List<JobResult>();
            var start = 0;

            while (results.Count < request.ResultWanted)
            {
                var url = BuildUrl(request, start);
                var html = await FetchAsync(client, url, ct);
                var jobs = ParseJobs(html);

                if (jobs.Count == 0) break;

                results.AddRange(jobs);
                start += PageSize;
            }

            return results.Take(request.ResultWanted).ToList();
        }, ct);
    }

    private static async Task<string> FetchAsync(HttpClient client, string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", "none");

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw new CrawlException("linkedin", $"LinkedIn returned {(int)response.StatusCode} ({response.ReasonPhrase})");

        return await response.Content.ReadAsStringAsync(ct);
    }

    private static string BuildUrl(CrawlRequest request, int start)
    {
        var location = BuildLocation(request.City, request.Country);

        var url = $"{BaseUrl}?keywords={Uri.EscapeDataString(request.SearchTerm)}" +
                  $"&location={Uri.EscapeDataString(location)}" +
                  $"&start={start}";

        if (request.JobType is { Count: > 0 })
            url += $"&f_JT={string.Join(",", request.JobType.Select(MapJobType))}";

        if (request.DatePosted.HasValue && request.DatePosted != EDatePosted.Anytime)
            url += $"&f_TPR={MapDatePosted(request.DatePosted.Value)}";

        if (request.Remote == true)
            url += "&f_WT=2";

        return url;
    }

    private static string BuildLocation(string? city, string? country)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(country)) parts.Add(country);
        return string.Join(", ", parts);
    }

    private static string MapJobType(EJobType type) => type switch
    {
        EJobType.FullTime => "F",
        EJobType.PartTime => "P",
        EJobType.Contract => "C",
        EJobType.Internship => "I",
        EJobType.Temporary => "T",
        _ => ""
    };

    private static string MapDatePosted(EDatePosted posted) => posted switch
    {
        EDatePosted.Past24Hours => "r86400",
        EDatePosted.PastWeek => "r604800",
        EDatePosted.PastMonth => "r2592000",
        _ => ""
    };

    private static List<JobResult> ParseJobs(string html)
    {
        var results = new List<JobResult>();
        var anchors = EntityUrnRegex().Matches(html);

        for (var i = 0; i < anchors.Count; i++)
        {
            var startIdx = anchors[i].Index;
            var endIdx = i + 1 < anchors.Count ? anchors[i + 1].Index : html.Length;
            var chunk = html[startIdx..endIdx];

            var title = Extract(TitleRegex(), chunk);
            if (string.IsNullOrWhiteSpace(title)) continue;

            var job = new JobResult
            {
                Site = ESite.LinkedIn,
                Title = WebUtility.HtmlDecode(title),
                Company = WebUtility.HtmlDecode(Extract(CompanyRegex(), chunk)),
                JobUrl = CleanUrl(Extract(JobUrlRegex(), chunk)),
                SourceId = anchors[i].Groups[1].Value
            };

            ParseLocation(Extract(LocationRegex(), chunk), job);

            var dateStr = Extract(DateRegex(), chunk);
            if (DateTime.TryParse(dateStr, out var date))
                job.PostedDate = date;

            results.Add(job);
        }

        return results;
    }

    private static void ParseLocation(string raw, JobResult job)
    {
        if (string.IsNullOrEmpty(raw)) return;

        var parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        job.Location = parts.Length switch
        {
            >= 3 => new LocationModel { City = parts[0], Country = parts[^1] },
            2 => new LocationModel { City = parts[0], Country = parts[1] },
            1 => new LocationModel { City = parts[0] },
            _ => null
        };
    }

    private static string CleanUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;
        var decoded = WebUtility.HtmlDecode(url);
        var qIndex = decoded.IndexOf('?');
        return qIndex > 0 ? decoded[..qIndex] : decoded;
    }

    private static string Extract(Regex regex, string input)
    {
        var match = regex.Match(input);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    [GeneratedRegex(@"data-entity-urn=""urn:li:jobPosting:(\d+)""", RegexOptions.Singleline)]
    private static partial Regex EntityUrnRegex();

    [GeneratedRegex(@"base-search-card__title[^>]*>\s*([^<]+)", RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"hidden-nested-link[^>]*>\s*([^<]+)", RegexOptions.Singleline)]
    private static partial Regex CompanyRegex();

    [GeneratedRegex(@"job-search-card__location[^>]*>\s*([^<]+)", RegexOptions.Singleline)]
    private static partial Regex LocationRegex();

    [GeneratedRegex(@"base-card__full-link[^>]*href=""([^""]+)""", RegexOptions.Singleline)]
    private static partial Regex JobUrlRegex();

    [GeneratedRegex(@"datetime=""([^""]+)""", RegexOptions.Singleline)]
    private static partial Regex DateRegex();
}
