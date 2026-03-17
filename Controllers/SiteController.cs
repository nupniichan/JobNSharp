using JobNSharp.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobNSharp.Controllers;

[ApiController]
[Route("api/sites")]
public class SiteController(IJobProviderFactory factory) : ControllerBase
{
    [HttpGet]
    public IActionResult GetSites()
    {
        return Ok(factory.GetAvailableSites());
    }
}
