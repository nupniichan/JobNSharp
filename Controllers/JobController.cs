using JobNSharp.Interfaces;
using JobNSharp.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobNSharp.Controllers;

[ApiController]
[Route("api/job")]
public class JobController(IJobCrawlService crawlService) : ControllerBase
{
    [HttpPost("crawl")]
    public async Task<IActionResult> Crawl([FromBody] CrawlRequest request, CancellationToken ct)
    {
        var results = await crawlService.CrawlAsync(request, ct);
        return Ok(results);
    }

    [HttpPost("crawl-all")]
    public async Task<IActionResult> CrawlAll([FromBody] CrawlAllRequest request, CancellationToken ct)
    {
        var results = await crawlService.CrawlAllAsync(request, ct);
        return Ok(results);
    }
}
