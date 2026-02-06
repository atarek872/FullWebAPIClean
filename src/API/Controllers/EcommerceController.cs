using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EcommerceController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    public EcommerceController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize(Policy = "Seller")]
    [HttpPost("seller/profile")]
    public async Task<IActionResult> CreateOrUpdateSellerProfile([FromBody] UpsertSellerProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var profile = await _dbContext.SellerProfiles.FirstOrDefaultAsync(x => x.UserId == userId.Value, cancellationToken);
        if (profile is null)
        {
            profile = new SellerProfile
            {
                UserId = userId.Value,
                StoreName = request.StoreName,
                StoreDescription = request.StoreDescription,
                Slug = request.Slug,
                IsPublished = request.IsPublished
            };
            _dbContext.SellerProfiles.Add(profile);
        }
        else
        {
            profile.StoreName = request.StoreName;
            profile.StoreDescription = request.StoreDescription;
            profile.Slug = request.Slug;
            profile.IsPublished = request.IsPublished;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new { profile.Id, profile.StoreName, profile.Slug, profile.IsPublished });
    }

    [Authorize(Policy = "Seller")]
    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var sellerProfile = await _dbContext.SellerProfiles.FirstOrDefaultAsync(x => x.UserId == userId.Value && x.IsPublished, cancellationToken);
        if (sellerProfile is null)
        {
            return BadRequest("Seller profile was not found or is not published.");
        }

        var product = new Product
        {
            SellerProfileId = sellerProfile.Id,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            BasePrice = request.BasePrice,
            DiscountPercentage = request.DiscountPercentage,
            IsActive = true,
            Images = request.Images.Select((image, index) => new ProductImage
            {
                ImageUrl = image.ImageUrl,
                AltText = image.AltText,
                SortOrder = index
            }).ToList(),
            CustomFields = request.CustomFields.Select(field => new ProductCustomField
            {
                Key = field.Key,
                Label = field.Label,
                InputType = field.InputType,
                IsRequired = field.IsRequired,
                Placeholder = field.Placeholder,
                AllowedOptionsCsv = field.AllowedOptions is { Count: > 0 } ? string.Join(',', field.AllowedOptions) : null
            }).ToList(),
            PromoCodes = request.PromoCodes.Select(promo => new PromoCode
            {
                Code = promo.Code.ToUpperInvariant(),
                DiscountPercentage = promo.DiscountPercentage,
                StartsAtUtc = promo.StartsAtUtc,
                EndsAtUtc = promo.EndsAtUtc,
                IsActive = promo.IsActive
            }).ToList()
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { product.Id, product.Name, product.BasePrice, Images = product.Images.Count, CustomFields = product.CustomFields.Count });
    }

    [AllowAnonymous]
    [HttpGet("products/search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var terms = (query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.SellerProfile != null && x.SellerProfile.IsPublished)
            .Include(x => x.SellerProfile)
            .Include(x => x.Images)
            .ToListAsync(cancellationToken);

        var ranked = products
            .Select(product =>
            {
                var source = $"{product.Name} {product.Description} {product.Category} {product.SellerProfile!.StoreName}".ToLowerInvariant();
                var score = terms.Count == 0 ? 1 : terms.Count(term => source.Contains(term));
                return new
                {
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Category,
                    product.BasePrice,
                    product.DiscountPercentage,
                    Seller = product.SellerProfile!.StoreName,
                    Thumbnail = product.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).FirstOrDefault(),
                    SearchScore = score
                };
            })
            .Where(x => x.SearchScore > 0)
            .OrderByDescending(x => x.SearchScore)
            .ThenBy(x => x.Name)
            .ToList();

        var total = ranked.Count;
        var paged = ranked.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new { total, page, pageSize, items = paged });
    }

    [AllowAnonymous]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return BadRequest("At least one item is required.");
        }

        var productIds = request.Items.Select(x => x.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Include(x => x.CustomFields)
            .Include(x => x.PromoCodes)
            .Where(x => productIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (products.Count != productIds.Count)
        {
            return BadRequest("One or more products are not available.");
        }

        var order = new Order
        {
            BuyerUserId = GetUserId(),
            BuyerName = request.BuyerName,
            BuyerEmail = request.BuyerEmail,
            Status = "Pending"
        };

        decimal subtotal = 0m;
        decimal discountTotal = 0m;

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            var unitPrice = product.BasePrice;

            var productDiscount = product.DiscountPercentage ?? 0;
            var promoDiscount = 0m;
            if (!string.IsNullOrWhiteSpace(item.PromoCode))
            {
                var promo = product.PromoCodes.FirstOrDefault(x => x.Code == item.PromoCode.ToUpperInvariant() && x.IsActive && x.StartsAtUtc <= DateTime.UtcNow && x.EndsAtUtc >= DateTime.UtcNow);
                promoDiscount = promo?.DiscountPercentage ?? 0;
            }

            var effectiveDiscount = Math.Min(90m, productDiscount + promoDiscount);
            var discountedUnitPrice = Math.Round(unitPrice * (1 - effectiveDiscount / 100m), 2);

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                DiscountedUnitPrice = discountedUnitPrice,
                CustomFieldValues = item.CustomFieldValues.Select(value => new OrderItemCustomFieldValue
                {
                    FieldKey = value.FieldKey,
                    Value = value.Value
                }).ToList()
            };

            var availableKeys = product.CustomFields.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var invalidKeys = orderItem.CustomFieldValues.Where(x => !availableKeys.Contains(x.FieldKey)).Select(x => x.FieldKey).Distinct().ToList();
            if (invalidKeys.Count > 0)
            {
                return BadRequest($"Invalid custom field keys for product '{product.Name}': {string.Join(',', invalidKeys)}");
            }

            var requiredKeys = product.CustomFields.Where(x => x.IsRequired).Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var submittedKeys = orderItem.CustomFieldValues.Select(x => x.FieldKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!requiredKeys.IsSubsetOf(submittedKeys))
            {
                return BadRequest($"Missing required custom fields for product '{product.Name}'.");
            }

            subtotal += unitPrice * item.Quantity;
            discountTotal += (unitPrice - discountedUnitPrice) * item.Quantity;
            order.Items.Add(orderItem);
        }

        order.Subtotal = Math.Round(subtotal, 2);
        order.DiscountAmount = Math.Round(discountTotal, 2);
        order.Total = Math.Round(subtotal - discountTotal, 2);

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { order.Id, order.Subtotal, order.DiscountAmount, order.Total, order.Status });
    }

    private Guid? GetUserId()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdRaw, out var userId) ? userId : null;
    }
}

public class UpsertSellerProfileRequest
{
    [Required, MaxLength(200)]
    public string StoreName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? StoreDescription { get; set; }

    [MaxLength(300)]
    public string? Slug { get; set; }

    public bool IsPublished { get; set; } = true;
}

public class CreateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(0.01, 100000000)]
    public decimal BasePrice { get; set; }

    [Range(0, 90)]
    public decimal? DiscountPercentage { get; set; }

    public List<CreateProductImageRequest> Images { get; set; } = [];
    public List<CreateProductCustomFieldRequest> CustomFields { get; set; } = [];
    public List<CreatePromoCodeRequest> PromoCodes { get; set; } = [];
}

public class CreateProductImageRequest
{
    [Required, MaxLength(1000)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? AltText { get; set; }
}

public class CreateProductCustomFieldRequest
{
    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Label { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string InputType { get; set; } = "text";

    public bool IsRequired { get; set; }

    [MaxLength(500)]
    public string? Placeholder { get; set; }

    public List<string>? AllowedOptions { get; set; }
}

public class CreatePromoCodeRequest
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Range(0.01, 90)]
    public decimal DiscountPercentage { get; set; }

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CheckoutRequest
{
    [Required, MaxLength(200)]
    public string BuyerName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(300)]
    public string BuyerEmail { get; set; } = string.Empty;

    public List<CheckoutItemRequest> Items { get; set; } = [];
}

public class CheckoutItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [MaxLength(50)]
    public string? PromoCode { get; set; }

    public List<CheckoutCustomFieldValueRequest> CustomFieldValues { get; set; } = [];
}

public class CheckoutCustomFieldValueRequest
{
    [Required, MaxLength(100)]
    public string FieldKey { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Value { get; set; } = string.Empty;
}
