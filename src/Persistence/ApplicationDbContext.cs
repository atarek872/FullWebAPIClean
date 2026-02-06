using Application.Common.Interfaces;
using Application.Common.Interfaces.MultiTenancy;
using Domain.Entities;
using Domain.Entities.Multitenancy;
using Domain.MultiTenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public string TenantSchema => _tenantContext.IsResolved ? _tenantContext.TenantSchema : "public";

    public DbSet<SellerProfile> SellerProfiles => Set<SellerProfile>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductCustomField> ProductCustomFields => Set<ProductCustomField>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemCustomFieldValue> OrderItemCustomFieldValues => Set<OrderItemCustomFieldValue>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<TenantFeature> TenantFeatures => Set<TenantFeature>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<UserTenantMembership> UserTenantMemberships => Set<UserTenantMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(TenantSchema);

        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(r => !r.IsDeleted);
        modelBuilder.Entity<Tenant>().HasQueryFilter(t => !t.IsDeleted);

        ConfigureTenantEntities(modelBuilder);
        ConfigureEcommerceEntities(modelBuilder);
    }

    private void ConfigureTenantEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>().ToTable("Tenants", "public");
        modelBuilder.Entity<Plan>().ToTable("Plans", "public");
        modelBuilder.Entity<Feature>().ToTable("Features", "public");
        modelBuilder.Entity<TenantFeature>().ToTable("TenantFeatures", "public");
        modelBuilder.Entity<TenantSubscription>().ToTable("TenantSubscriptions", "public");
        modelBuilder.Entity<UsageRecord>().ToTable("UsageRecords", "public");
        modelBuilder.Entity<UserTenantMembership>().ToTable("UserTenantMemberships", "public");

        modelBuilder.Entity<Tenant>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<Tenant>().HasIndex(x => x.Subdomain).IsUnique();
        modelBuilder.Entity<Feature>().HasIndex(x => x.Code).IsUnique();

        modelBuilder.Entity<TenantFeature>().HasIndex(x => new { x.TenantId, x.FeatureId }).IsUnique();
        modelBuilder.Entity<UserTenantMembership>().HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
    }

    private void ConfigureEcommerceEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SellerProfile>().HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Product>().HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ProductImage>().HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ProductCustomField>().HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<PromoCode>().HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Order>().HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<OrderItemCustomFieldValue>().HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantContext.TenantId);
    }
}
