using JobNSharp.Models;
using JobNSharp.Models.Enums;

namespace JobNSharp.Interfaces;

public interface IJobProvider
{
    ESite SiteName { get; }
    Task<List<JobResult>> CrawlAsync(CrawlRequest request, CancellationToken ct = default);
}
