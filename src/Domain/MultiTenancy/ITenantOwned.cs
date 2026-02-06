namespace Domain.MultiTenancy;

public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
