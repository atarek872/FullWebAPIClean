namespace Application.Common.Interfaces.MultiTenancy;

public interface ITenantCache
{
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
}
