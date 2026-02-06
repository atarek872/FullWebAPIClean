using Domain.Entities.Multitenancy;
using Microsoft.Extensions.Logging;
using Worker.Services;

namespace Worker.Jobs;

public class EmailDigestJob : ITenantJob
{
    private readonly ILogger<EmailDigestJob> _logger;
    public EmailDigestJob(ILogger<EmailDigestJob> logger) => _logger = logger;
    public Task ExecuteAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running email digest for tenant {TenantId}", tenant.Id);
        return Task.CompletedTask;
    }
}
