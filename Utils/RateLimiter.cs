using JobNSharp.Interfaces;
using System.Collections.Concurrent;

namespace JobNSharp.Utils;

public class RateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly int _maxConcurrency;

    public RateLimiter(IConfiguration configuration)
    {
        _maxConcurrency = configuration.GetValue("RateLimit:MaxConcurrentRequests", 3);
    }

    public async Task<T> ExecuteAsync<T>(string key, Func<Task<T>> action, CancellationToken ct = default)
    {
        var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(_maxConcurrency, _maxConcurrency));
        await semaphore.WaitAsync(ct);
        try
        {
            return await action();
        }
        finally
        {
            semaphore.Release();
        }
    }
}
