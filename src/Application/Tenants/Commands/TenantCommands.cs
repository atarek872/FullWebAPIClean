using Application.Common.Interfaces;
using Application.Tenants.DTOs;
using Domain.Entities.Multitenancy;
using Domain.MultiTenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Tenants.Commands;

public sealed record CreateTenantCommand(CreateTenantRequest Request) : IRequest<Guid>;
public sealed record UpdateTenantSettingsCommand(UpdateTenantSettingsRequest Request) : IRequest;
public sealed record AssignPlanCommand(AssignPlanRequest Request) : IRequest;
public sealed record SuspendTenantCommand(SuspendTenantRequest Request) : IRequest;

public class CreateTenantHandler : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    public CreateTenantHandler(IApplicationDbContext db) => _db = db;
    public async Task<Guid> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var plan = await _db.Plans.FirstAsync(x => x.Id == request.Request.PlanId, cancellationToken);
        var tenant = new Tenant
        {
            Name = request.Request.Name,
            Slug = request.Request.Slug,
            Subdomain = request.Request.Subdomain,
            Schema = $"{request.Request.Slug.ToLowerInvariant()}_schema",
            Plan = plan.PlanType,
            ApiRequestLimitPerDay = plan.ApiRequestLimitPerDay,
            StorageLimitMb = plan.StorageLimitMb
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);
        return tenant.Id;
    }
}

public class UpdateTenantSettingsHandler : IRequestHandler<UpdateTenantSettingsCommand>
{
    private readonly IApplicationDbContext _db;
    public UpdateTenantSettingsHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(UpdateTenantSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.FirstAsync(x => x.Id == request.Request.TenantId, cancellationToken);
        tenant.SettingsJson = request.Request.SettingsJson;
        tenant.ApiRequestLimitPerDay = request.Request.ApiRequestLimitPerDay;
        tenant.StorageLimitMb = request.Request.StorageLimitMb;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class AssignPlanHandler : IRequestHandler<AssignPlanCommand>
{
    private readonly IApplicationDbContext _db;
    public AssignPlanHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(AssignPlanCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.FirstAsync(x => x.Id == request.Request.TenantId, cancellationToken);
        var plan = await _db.Plans.FirstAsync(x => x.Id == request.Request.PlanId, cancellationToken);
        tenant.Plan = plan.PlanType;
        tenant.ApiRequestLimitPerDay = plan.ApiRequestLimitPerDay;
        tenant.StorageLimitMb = plan.StorageLimitMb;
        _db.TenantSubscriptions.Add(new TenantSubscription
        {
            TenantId = tenant.Id,
            PlanId = plan.Id,
            StartsAtUtc = DateTime.UtcNow,
            IsActive = true,
            BillingProviderSubscriptionId = $"local_{tenant.Id:N}_{plan.Id:N}"
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class SuspendTenantHandler : IRequestHandler<SuspendTenantCommand>
{
    private readonly IApplicationDbContext _db;
    public SuspendTenantHandler(IApplicationDbContext db) => _db = db;
    public async Task Handle(SuspendTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants.FirstAsync(x => x.Id == request.Request.TenantId, cancellationToken);
        tenant.Status = TenantStatus.Suspended;
        tenant.SettingsJson = $"{{\"suspensionReason\":\"{request.Request.Reason}\"}}";
        await _db.SaveChangesAsync(cancellationToken);
    }
}
