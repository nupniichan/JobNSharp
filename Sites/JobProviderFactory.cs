using JobNSharp.Interfaces;
using JobNSharp.Models.Enums;
using JobNSharp.Utils.Exceptions;

namespace JobNSharp.Sites;

public class JobProviderFactory : IJobProviderFactory
{
    private readonly Dictionary<ESite, IJobProvider> _providers;

    public JobProviderFactory(IEnumerable<IJobProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.SiteName);
    }

    public IJobProvider GetProvider(ESite site)
    {
        if (!_providers.TryGetValue(site, out var provider))
            throw new CrawlException(site.ToString(), $"Provider '{site}' is not supported. Available: {string.Join(", ", _providers.Keys)}");

        return provider;
    }

    public IReadOnlyList<ESite> GetAvailableSites() => _providers.Keys.ToList();

    public IReadOnlyList<IJobProvider> GetAllProviders() => _providers.Values.ToList();
}
