using JobNSharp.Models.Enums;

namespace JobNSharp.Models;

public class CrawlAllRequest
{
    public required string SearchTerm { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public List<EJobType>? JobType { get; set; }
    public EDatePosted? DatePosted { get; set; }
    public bool? Remote { get; set; }
    public bool IncludeDescription { get; set; }
    public int ResultWanted { get; set; } = 10;

    public CrawlRequest ToCrawlRequest(ESite site) => new()
    {
        SearchTerm = SearchTerm,
        City = City,
        Country = Country,
        JobType = JobType,
        DatePosted = DatePosted,
        Remote = Remote,
        IncludeDescription = IncludeDescription,
        Site = site,
        ResultWanted = ResultWanted
    };
}
