using JobNSharp.Interfaces;
using JobNSharp.Models;
using Microsoft.AspNetCore.Mvc;

namespace JobNSharp.Controllers;

[ApiController]
[Route("api/job")]
public class JobController(IJobCrawlService crawlService) : ControllerBase
{
    [HttpGet("crawl")]
    public async Task<IActionResult> Crawl([FromQuery] CrawlRequest request, CancellationToken ct)
    {
        var results = await crawlService.CrawlAsync(request, ct);
        return Ok(results);
    }

    [HttpGet("crawl-all")]
    public async Task<IActionResult> CrawlAll([FromQuery] CrawlAllRequest request, CancellationToken ct)
    {
        var results = await crawlService.CrawlAllAsync(request, ct);
        return Ok(results);
    }
}
