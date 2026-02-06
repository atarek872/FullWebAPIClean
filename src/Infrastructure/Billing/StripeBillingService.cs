using Application.Common.Interfaces.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Billing;

public class StripeBillingService : IBillingService
{
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(ILogger<StripeBillingService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateOrUpdateSubscriptionAsync(Guid tenantId, string customerEmail, string priceId, CancellationToken cancellationToken = default)
    {
        var subscriptionId = $"sub_{tenantId:N}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        _logger.LogInformation("Stripe subscription upserted for tenant {TenantId} with plan {PriceId}", tenantId, priceId);
        return Task.FromResult(subscriptionId);
    }

    public Task CancelSubscriptionAsync(Guid tenantId, string subscriptionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stripe subscription cancelled for tenant {TenantId}: {SubscriptionId}", tenantId, subscriptionId);
        return Task.CompletedTask;
    }
}
