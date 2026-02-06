using Application.Common.Interfaces.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Worker.Services;

namespace Worker;

public class BackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundWorker> _logger;

    public BackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<BackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            var jobs = scope.ServiceProvider.GetServices<ITenantJob>().ToArray();
            var tenants = await db.Tenants.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync(stoppingToken);

            foreach (var tenant in tenants)
            {
                tenantContext.SetTenant(new TenantContextSnapshot(tenant.Id, tenant.Schema, tenant.Plan, tenant.Status, tenant.ApiRequestLimitPerDay, tenant.StorageLimitMb));
                foreach (var job in jobs)
                {
                    await job.ExecuteAsync(tenant, stoppingToken);
                }
            }

            _logger.LogInformation("Tenant iteration complete at {Time}", DateTimeOffset.UtcNow);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
