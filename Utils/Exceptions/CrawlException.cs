namespace JobNSharp.Utils.Exceptions;

public class CrawlException : Exception
{
    public string? Site { get; }

    public CrawlException(string message) : base(message) { }

    public CrawlException(string site, string message) : base(message)
    {
        Site = site;
    }

    public CrawlException(string site, string message, Exception innerException)
        : base(message, innerException)
    {
        Site = site;
    }
}
