using Application.Common.Interfaces.MultiTenancy;

namespace Infrastructure.MultiTenancy;

public class TenantStorageService : ITenantStorageService
{
    private readonly ITenantContext _tenantContext;

    public TenantStorageService(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "storage", _tenantContext.TenantId.ToString("N"));
        Directory.CreateDirectory(root);
        var fullPath = Path.Combine(root, fileName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);
        return fullPath;
    }
}
