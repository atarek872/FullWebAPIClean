using Domain.MultiTenancy;

namespace Domain.Entities.Multitenancy;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string Schema { get; set; } = string.Empty;
    public PlanType Plan { get; set; } = PlanType.Free;
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public int ApiRequestLimitPerDay { get; set; } = 1000;
    public long StorageLimitMb { get; set; } = 1024;
    public string SettingsJson { get; set; } = "{}";

    public ICollection<TenantFeature> Features { get; set; } = [];
    public ICollection<TenantSubscription> Subscriptions { get; set; } = [];
}
