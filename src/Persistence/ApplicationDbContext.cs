using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<SellerProfile> SellerProfiles => Set<SellerProfile>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductCustomField> ProductCustomFields => Set<ProductCustomField>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemCustomFieldValue> OrderItemCustomFieldValues => Set<OrderItemCustomFieldValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Role>().HasQueryFilter(r => !r.IsDeleted);

        modelBuilder.Entity<SellerProfile>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<SellerProfile>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .Property(x => x.BasePrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(x => x.DiscountPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Product>()
            .HasOne(x => x.SellerProfile)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.SellerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductImage>()
            .HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductCustomField>()
            .HasOne(x => x.Product)
            .WithMany(x => x.CustomFields)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PromoCode>()
            .Property(x => x.DiscountPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<PromoCode>()
            .HasIndex(x => new { x.ProductId, x.Code })
            .IsUnique();

        modelBuilder.Entity<PromoCode>()
            .HasOne(x => x.Product)
            .WithMany(x => x.PromoCodes)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .Property(x => x.Subtotal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(x => x.DiscountAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(x => x.Total)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.BuyerUser)
            .WithMany()
            .HasForeignKey(x => x.BuyerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OrderItem>()
            .Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(x => x.DiscountedUnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItemCustomFieldValue>()
            .HasOne(x => x.OrderItem)
            .WithMany(x => x.CustomFieldValues)
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
