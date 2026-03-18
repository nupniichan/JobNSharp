using JobNSharp.Models.Enums;

namespace JobNSharp.Interfaces;

public interface IJobProviderFactory
{
    IJobProvider GetProvider(ESite site);
    IReadOnlyList<ESite> GetAvailableSites();
    IReadOnlyList<IJobProvider> GetAllProviders();
}
