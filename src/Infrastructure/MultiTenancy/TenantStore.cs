using Application.Common.Interfaces.MultiTenancy;
using Domain.Entities.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Persistence;

namespace Infrastructure.MultiTenancy;

public class TenantStore : ITenantStore
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public TenantStore(ApplicationDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => GetCachedAsync($"tenant:id:{tenantId}",
            () => _dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId && !x.IsDeleted, cancellationToken));

    public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
        => GetCachedAsync($"tenant:sub:{subdomain.ToLowerInvariant()}",
            () => _dbContext.Tenants.FirstOrDefaultAsync(x => x.Subdomain == subdomain && !x.IsDeleted, cancellationToken));

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => GetCachedAsync($"tenant:slug:{slug.ToLowerInvariant()}",
            () => _dbContext.Tenants.FirstOrDefaultAsync(x => x.Slug == slug && !x.IsDeleted, cancellationToken));

    private async Task<Tenant?> GetCachedAsync(string key, Func<Task<Tenant?>> factory)
    {
        if (_cache.TryGetValue(key, out Tenant? tenant))
        {
            return tenant;
        }

        tenant = await factory();
        if (tenant is not null)
        {
            _cache.Set(key, tenant, TimeSpan.FromMinutes(10));
        }

        return tenant;
    }
}
