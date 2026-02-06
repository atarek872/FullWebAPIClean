using Domain.Entities.Multitenancy;
using Microsoft.Extensions.Logging;
using Worker.Services;

namespace Worker.Jobs;

public class CleanupInactiveItemsJob : ITenantJob
{
    private readonly ILogger<CleanupInactiveItemsJob> _logger;
    public CleanupInactiveItemsJob(ILogger<CleanupInactiveItemsJob> logger) => _logger = logger;
    public Task ExecuteAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running cleanup inactive items for tenant {TenantId}", tenant.Id);
        return Task.CompletedTask;
    }
}
