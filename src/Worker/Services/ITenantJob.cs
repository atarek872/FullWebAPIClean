using Domain.Entities.Multitenancy;

namespace Worker.Services;

public interface ITenantJob
{
    Task ExecuteAsync(Tenant tenant, CancellationToken cancellationToken);
}
