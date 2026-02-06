using Application.Common.Interfaces.MultiTenancy;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.MultiTenancy;

public class TenantMemoryCache : ITenantCache
{
    private readonly IMemoryCache _cache;
    private readonly ITenantContext _tenantContext;

    public TenantMemoryCache(IMemoryCache cache, ITenantContext tenantContext)
    {
        _cache = cache;
        _tenantContext = tenantContext;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _cache.Set(BuildKey(key), value, ttl);
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(BuildKey(key), out T? value);
        return Task.FromResult(value);
    }

    private string BuildKey(string key) => $"{_tenantContext.TenantId}:{key}";
}
