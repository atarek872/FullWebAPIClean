namespace Application.Common.Interfaces.MultiTenancy;

public interface ITenantStorageService
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
}
