using Domain.MultiTenancy;

namespace Domain.Entities.Multitenancy;

public class Plan : BaseEntity
{
    public PlanType PlanType { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public int ApiRequestLimitPerDay { get; set; }
    public long StorageLimitMb { get; set; }
}
