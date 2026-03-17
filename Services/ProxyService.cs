using JobNSharp.Interfaces;
using System.Net;

namespace JobNSharp.Utils;

public class ProxyService : IProxyService
{
    private readonly WebProxy[] _proxies;
    private int _currentIndex = -1;

    public ProxyService(IConfiguration configuration)
    {
        var urls = configuration.GetSection("Proxy:Urls").Get<string[]>() ?? [];
        _proxies = urls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => new WebProxy(u))
            .ToArray();
    }

    public IWebProxy? GetNextProxy()
    {
        if (_proxies.Length == 0)
            return null;

        var index = Interlocked.Increment(ref _currentIndex);
        return _proxies[(index & int.MaxValue) % _proxies.Length];
    }
}
