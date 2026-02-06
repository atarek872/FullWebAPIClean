using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class SellerProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(200)]
    public string StoreName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? StoreDescription { get; set; }

    [MaxLength(300)]
    public string? Slug { get; set; }

    public bool IsPublished { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
}

public class Product : BaseEntity
{
    public Guid SellerProfileId { get; set; }
    public SellerProfile? SellerProfile { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductCustomField> CustomFields { get; set; } = [];
    public ICollection<PromoCode> PromoCodes { get; set; } = [];
}

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    [MaxLength(1000)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }
}

public class ProductCustomField : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(30)]
    public string InputType { get; set; } = "text";

    public bool IsRequired { get; set; }

    [MaxLength(500)]
    public string? Placeholder { get; set; }

    [MaxLength(2000)]
    public string? AllowedOptionsCsv { get; set; }
}

public class PromoCode : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public decimal DiscountPercentage { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Order : BaseEntity
{
    public Guid? BuyerUserId { get; set; }
    public User? BuyerUser { get; set; }

    [MaxLength(200)]
    public string BuyerName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string BuyerEmail { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Status { get; set; } = "Pending";

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountedUnitPrice { get; set; }

    public ICollection<OrderItemCustomFieldValue> CustomFieldValues { get; set; } = [];
}

public class OrderItemCustomFieldValue : BaseEntity
{
    public Guid OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    [MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;
}
