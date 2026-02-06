namespace Application.Tenants.DTOs;

public sealed record CreateTenantRequest(string Name, string Slug, string Subdomain, Guid PlanId);
public sealed record UpdateTenantSettingsRequest(Guid TenantId, string SettingsJson, int ApiRequestLimitPerDay, long StorageLimitMb);
public sealed record AssignPlanRequest(Guid TenantId, Guid PlanId);
public sealed record SuspendTenantRequest(Guid TenantId, string Reason);
