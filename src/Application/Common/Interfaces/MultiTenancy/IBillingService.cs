namespace Application.Common.Interfaces.MultiTenancy;

public interface IBillingService
{
    Task<string> CreateOrUpdateSubscriptionAsync(Guid tenantId, string customerEmail, string priceId, CancellationToken cancellationToken = default);
    Task CancelSubscriptionAsync(Guid tenantId, string subscriptionId, CancellationToken cancellationToken = default);
}
