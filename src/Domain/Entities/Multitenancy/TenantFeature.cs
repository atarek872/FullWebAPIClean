namespace Domain.Entities.Multitenancy;

public class TenantFeature : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid FeatureId { get; set; }
    public Feature? Feature { get; set; }

    public bool IsEnabled { get; set; }
}
