using JobNSharp.Models;

namespace JobNSharp.Interfaces;

public interface IJobCrawlService
{
    Task<List<JobResult>> CrawlAsync(CrawlRequest request, CancellationToken ct = default);
    Task<List<JobResult>> CrawlAllAsync(CrawlAllRequest request, CancellationToken ct = default);
}
