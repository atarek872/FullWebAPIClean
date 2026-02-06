namespace Application.Common.Interfaces.MultiTenancy;

public interface ITenantOnboardingService
{
    Task RunAsync(Guid tenantId, string adminEmail, string adminPassword, CancellationToken cancellationToken = default);
}
