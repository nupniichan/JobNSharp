using JobNSharp.Interfaces;
using JobNSharp.Utils.Exceptions;

namespace JobNSharp.Sites;

public class JobProviderFactory : IJobProviderFactory
{
    private readonly Dictionary<string, IJobProvider> _providers;

    public JobProviderFactory(IEnumerable<IJobProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.SiteName, StringComparer.OrdinalIgnoreCase);
    }

    public IJobProvider GetProvider(string siteName)
    {
        if (!_providers.TryGetValue(siteName, out var provider))
            throw new CrawlException(siteName, $"Provider '{siteName}' is not supported. Available: {string.Join(", ", _providers.Keys)}");

        return provider;
    }

    public IReadOnlyList<string> GetAvailableSites() => _providers.Keys.ToList();

    public IReadOnlyList<IJobProvider> GetAllProviders() => _providers.Values.ToList();
}
