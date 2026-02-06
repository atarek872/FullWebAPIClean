using Domain.MultiTenancy;

namespace Application.Common.Interfaces.MultiTenancy;

public interface ITenantContext
{
    bool IsResolved { get; }
    Guid TenantId { get; }
    string TenantSchema { get; }
    PlanType Plan { get; }
    TenantStatus Status { get; }
    int ApiRequestLimitPerDay { get; }
    long StorageLimitMb { get; }

    void SetTenant(TenantContextSnapshot snapshot);
}

public sealed record TenantContextSnapshot(
    Guid TenantId,
    string TenantSchema,
    PlanType Plan,
    TenantStatus Status,
    int ApiRequestLimitPerDay,
    long StorageLimitMb);
