namespace Domain.Entities.Multitenancy;

public class UserTenantMembership : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Role { get; set; } = "User";
    public string PermissionsCsv { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
