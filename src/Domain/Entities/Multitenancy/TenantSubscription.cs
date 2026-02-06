namespace Domain.Entities.Multitenancy;

public class TenantSubscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public string BillingProviderSubscriptionId { get; set; } = string.Empty;
}
