using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<SellerProfile> SellerProfiles { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductCustomField> ProductCustomFields { get; }
    DbSet<PromoCode> PromoCodes { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderItemCustomFieldValue> OrderItemCustomFieldValues { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
