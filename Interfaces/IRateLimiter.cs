namespace JobNSharp.Interfaces
{
    public interface IRateLimiter
    {
        Task<T> ExecuteAsync<T>(string key, Func<Task<T>> action, CancellationToken ct = default);
    }
}
