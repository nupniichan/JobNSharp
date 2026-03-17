using JobNSharp.Models;

namespace JobNSharp.Interfaces;

public interface IJobProvider
{
    string SiteName { get; }
    Task<List<JobResult>> CrawlAsync(CrawlRequest request, CancellationToken ct = default);
}
