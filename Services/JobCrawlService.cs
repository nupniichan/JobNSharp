using JobNSharp.Interfaces;
using JobNSharp.Models;

namespace JobNSharp.Services;

public class JobCrawlService(IJobProviderFactory factory, ILogger<JobCrawlService> logger) : IJobCrawlService
{
    public async Task<List<JobResult>> CrawlAsync(CrawlRequest request, CancellationToken ct = default)
    {
        var provider = factory.GetProvider(request.Site);
        logger.LogInformation("Crawling {Site} for '{Search}'", request.Site, request.SearchTerm);
        return await provider.CrawlAsync(request, ct);
    }

    public async Task<List<JobResult>> CrawlAllAsync(CrawlAllRequest request, CancellationToken ct = default)
    {
        var providers = factory.GetAllProviders();
        logger.LogInformation("Crawling all {Count} sites for '{Search}'", providers.Count, request.SearchTerm);

        var tasks = providers.Select(p => CrawlSafeAsync(p, request.ToCrawlRequest(p.SiteName), ct));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private async Task<List<JobResult>> CrawlSafeAsync(IJobProvider provider, CrawlRequest request, CancellationToken ct)
    {
        try
        {
            return await provider.CrawlAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to crawl {Site}, skipping", provider.SiteName);
            return [];
        }
    }
}
