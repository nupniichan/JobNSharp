namespace JobNSharp.Interfaces;

public interface IJobProviderFactory
{
    IJobProvider GetProvider(string siteName);
    IReadOnlyList<string> GetAvailableSites();
    IReadOnlyList<IJobProvider> GetAllProviders();
}
