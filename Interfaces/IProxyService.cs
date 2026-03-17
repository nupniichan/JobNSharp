using System.Net;

namespace JobNSharp.Interfaces
{
    public interface IProxyService
    {
        IWebProxy? GetNextProxy();
    }
}
