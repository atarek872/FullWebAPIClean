using Application.Common.Interfaces.MultiTenancy;
using Domain.Entities.Multitenancy;
using Microsoft.Extensions.Logging;
using Worker.Services;

namespace Worker.Jobs;

public class BillingUsageSyncJob : ITenantJob
{
    private readonly IBillingService _billingService;
    private readonly ILogger<BillingUsageSyncJob> _logger;

    public BillingUsageSyncJob(IBillingService billingService, ILogger<BillingUsageSyncJob> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task ExecuteAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        await _billingService.CreateOrUpdateSubscriptionAsync(tenant.Id, $"billing@{tenant.Slug}.local", tenant.Plan.ToString(), cancellationToken);
        _logger.LogInformation("Synced billing usage for tenant {TenantId}", tenant.Id);
    }
}
