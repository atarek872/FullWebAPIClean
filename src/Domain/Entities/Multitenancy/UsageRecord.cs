using Domain.MultiTenancy;

namespace Domain.Entities.Multitenancy;

public class UsageRecord : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Metric { get; set; } = string.Empty;
    public long Amount { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public PlanType PlanAtCapture { get; set; }
}
