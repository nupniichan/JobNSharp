using JobNSharp.Models.Enums;

namespace JobNSharp.Models;

public class JobResult
{
    public string Site { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public LocationModel? Location { get; set; }
    public EJobType? JobType { get; set; }
    public string? Interval { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string JobUrl { get; set; } = string.Empty;
    public DateTime? PostedDate { get; set; }
    public string? Description { get; set; }
    public string? SourceId { get; set; }
}
