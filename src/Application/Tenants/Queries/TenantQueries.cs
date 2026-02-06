using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Tenants.Queries;

public sealed record GetTenantsQuery : IRequest<IReadOnlyCollection<TenantSummaryDto>>;
public sealed record TenantSummaryDto(Guid TenantId, string Name, string Slug, string Schema, string Plan, string Status);

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, IReadOnlyCollection<TenantSummaryDto>>
{
    private readonly IApplicationDbContext _db;
    public GetTenantsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<TenantSummaryDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
        => await _db.Tenants.AsNoTracking().Select(x => new TenantSummaryDto(x.Id, x.Name, x.Slug, x.Schema, x.Plan.ToString(), x.Status.ToString())).ToListAsync(cancellationToken);
}
