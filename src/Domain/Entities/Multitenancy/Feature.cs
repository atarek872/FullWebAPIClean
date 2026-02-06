namespace Domain.Entities.Multitenancy;

public class Feature : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool EnabledByDefault { get; set; }

    public ICollection<TenantFeature> Tenants { get; set; } = [];
}
