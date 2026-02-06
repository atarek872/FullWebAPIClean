using Application.Common.Interfaces.MultiTenancy;
using Domain.MultiTenancy;

namespace Infrastructure.MultiTenancy;

public class TenantContext : ITenantContext
{
    private TenantContextSnapshot? _snapshot;

    public bool IsResolved => _snapshot is not null;
    public Guid TenantId => _snapshot?.TenantId ?? Guid.Empty;
    public string TenantSchema => _snapshot?.TenantSchema ?? "public";
    public PlanType Plan => _snapshot?.Plan ?? PlanType.Free;
    public TenantStatus Status => _snapshot?.Status ?? TenantStatus.Disabled;
    public int ApiRequestLimitPerDay => _snapshot?.ApiRequestLimitPerDay ?? 0;
    public long StorageLimitMb => _snapshot?.StorageLimitMb ?? 0;

    public void SetTenant(TenantContextSnapshot snapshot) => _snapshot = snapshot;
}
