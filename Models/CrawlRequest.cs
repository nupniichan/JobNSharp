using JobNSharp.Models.Enums;

namespace JobNSharp.Models;

public class CrawlRequest
{
    public required string SearchTerm { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public List<EJobType>? JobType { get; set; }
    public EDatePosted? DatePosted { get; set; }
    public bool? Remote { get; set; }
    public bool IncludeDescription { get; set; }
    public required ESite Site { get; set; }
    public int ResultWanted { get; set; } = 10;
}
