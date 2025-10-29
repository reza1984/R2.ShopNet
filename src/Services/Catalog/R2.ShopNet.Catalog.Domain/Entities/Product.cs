using R2.ShopNet.Catalog.Domain.Enums;
using R2.ShopNet.Catalog.Domain.ValueObjects;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Domain.Entities;

/// <summary>
/// Represents a product in the catalog.
/// </summary>
public class Product : AuditableSoftDeletableEntity
{
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductVariant> _variants = [];

    /// <summary>
    /// The product name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The product description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Short description or summary of the product.
    /// </summary>
    public string? ShortDescription { get; private set; }

    /// <summary>
    /// The URL-friendly slug for the product.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Stock Keeping Unit - unique identifier for inventory management.
    /// </summary>
    public string Sku { get; private set; } = string.Empty;

    /// <summary>
    /// The product price.
    /// </summary>
    public Money Price { get; private set; } = Money.Zero();

    /// <summary>
    /// The discounted price, if applicable.
    /// </summary>
    public Money? DiscountPrice { get; private set; }

    /// <summary>
    /// The cost price (for profit calculation).
    /// </summary>
    public Money? CostPrice { get; private set; }

    /// <summary>
    /// Current stock quantity.
    /// </summary>
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Minimum stock quantity before reorder alert.
    /// </summary>
    public int ReorderLevel { get; private set; }

    /// <summary>
    /// Current product status.
    /// </summary>
    public ProductStatus Status { get; private set; }

    /// <summary>
    /// Category ID this product belongs to.
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Category this product belongs to.
    /// </summary>
    public Category Category { get; private set; } = null!;

    /// <summary>
    /// Brand name.
    /// </summary>
    public string? Brand { get; private set; }

    /// <summary>
    /// Product weight in grams.
    /// </summary>
    public decimal? Weight { get; private set; }

    /// <summary>
    /// Product dimensions (e.g., "10x20x30 cm").
    /// </summary>
    public string? Dimensions { get; private set; }

    /// <summary>
    /// Product images.
    /// </summary>
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    /// <summary>
    /// Product variants (e.g., different sizes, colors).
    /// </summary>
    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    /// <summary>
    /// SEO meta title.
    /// </summary>
    public string? MetaTitle { get; private set; }

    /// <summary>
    /// SEO meta description.
    /// </summary>
    public string? MetaDescription { get; private set; }

    /// <summary>
    /// SEO meta keywords.
    /// </summary>
    public string? MetaKeywords { get; private set; }

    /// <summary>
    /// Number of times this product has been viewed.
    /// </summary>
    public int ViewCount { get; private set; }

    /// <summary>
    /// Average rating (0-5).
    /// </summary>
    public decimal AverageRating { get; private set; }

    /// <summary>
    /// Number of reviews.
    /// </summary>
    public int ReviewCount { get; private set; }

    private Product() { }

    public Product(
        string name,
        string slug,
        string sku,
        Money price,
        Guid categoryId,
        string? description = null)
    {
        SetName(name);
        SetSlug(slug);
        SetSku(sku);
        SetPrice(price);
        CategoryId = categoryId;
        Description = description;
        Status = ProductStatus.Draft;
        StockQuantity = 0;
        ReorderLevel = 10;
        ViewCount = 0;
        AverageRating = 0;
        ReviewCount = 0;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(name));

        Name = name;
    }

    public void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Product slug cannot be empty", nameof(slug));

        if (slug.Length > 250)
            throw new ArgumentException("Product slug cannot exceed 250 characters", nameof(slug));

        Slug = slug.ToLowerInvariant();
    }

    public void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("Product SKU cannot be empty", nameof(sku));

        if (sku.Length > 50)
            throw new ArgumentException("Product SKU cannot exceed 50 characters", nameof(sku));

        Sku = sku.ToUpperInvariant();
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }

    public void SetShortDescription(string? shortDescription)
    {
        if (shortDescription != null && shortDescription.Length > 500)
            throw new ArgumentException("Short description cannot exceed 500 characters", nameof(shortDescription));

        ShortDescription = shortDescription;
    }

    public void SetPrice(Money price)
    {
        if (price.Amount < 0)
            throw new ArgumentException("Price cannot be negative", nameof(price));

        Price = price;
    }

    public void SetDiscountPrice(Money? discountPrice)
    {
        if (discountPrice != null && discountPrice.Amount >= Price.Amount)
            throw new ArgumentException("Discount price must be less than regular price", nameof(discountPrice));

        DiscountPrice = discountPrice;
    }

    public void SetCostPrice(Money? costPrice)
    {
        CostPrice = costPrice;
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(quantity));

        StockQuantity = quantity;

        // Auto-update status based on stock
        if (StockQuantity == 0 && Status == ProductStatus.Active)
        {
            Status = ProductStatus.OutOfStock;
        }
        else if (StockQuantity > 0 && Status == ProductStatus.OutOfStock)
        {
            Status = ProductStatus.Active;
        }
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        UpdateStock(StockQuantity + quantity);
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock");

        UpdateStock(StockQuantity - quantity);
    }

    public void SetReorderLevel(int reorderLevel)
    {
        if (reorderLevel < 0)
            throw new ArgumentException("Reorder level cannot be negative", nameof(reorderLevel));

        ReorderLevel = reorderLevel;
    }

    public void SetStatus(ProductStatus status)
    {
        Status = status;
    }

    public void Activate()
    {
        if (StockQuantity > 0)
            Status = ProductStatus.Active;
        else
            Status = ProductStatus.OutOfStock;
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
    }

    public void Discontinue()
    {
        Status = ProductStatus.Discontinued;
    }

    public void SetCategory(Guid categoryId)
    {
        CategoryId = categoryId;
    }

    public void SetBrand(string? brand)
    {
        if (brand != null && brand.Length > 100)
            throw new ArgumentException("Brand name cannot exceed 100 characters", nameof(brand));

        Brand = brand;
    }

    public void SetWeight(decimal? weight)
    {
        if (weight < 0)
            throw new ArgumentException("Weight cannot be negative", nameof(weight));

        Weight = weight;
    }

    public void SetDimensions(string? dimensions)
    {
        if (dimensions != null && dimensions.Length > 50)
            throw new ArgumentException("Dimensions cannot exceed 50 characters", nameof(dimensions));

        Dimensions = dimensions;
    }

    public void AddImage(ProductImage image)
    {
        _images.Add(image);
    }

    public void RemoveImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
            _images.Remove(image);
    }

    public void AddVariant(ProductVariant variant)
    {
        _variants.Add(variant);
    }

    public void RemoveVariant(Guid variantId)
    {
        var variant = _variants.FirstOrDefault(v => v.Id == variantId);
        if (variant != null)
            _variants.Remove(variant);
    }

    public void SetMetaTags(string? metaTitle, string? metaDescription, string? metaKeywords)
    {
        if (metaTitle != null && metaTitle.Length > 60)
            throw new ArgumentException("Meta title cannot exceed 60 characters", nameof(metaTitle));

        if (metaDescription != null && metaDescription.Length > 160)
            throw new ArgumentException("Meta description cannot exceed 160 characters", nameof(metaDescription));

        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        MetaKeywords = metaKeywords;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    public void UpdateRating(decimal newRating, int newReviewCount)
    {
        if (newRating < 0 || newRating > 5)
            throw new ArgumentException("Rating must be between 0 and 5", nameof(newRating));

        AverageRating = newRating;
        ReviewCount = newReviewCount;
    }

    public bool IsLowStock() => StockQuantity <= ReorderLevel && StockQuantity > 0;

    public bool IsOutOfStock() => StockQuantity == 0;

    public Money GetEffectivePrice() => DiscountPrice ?? Price;

    public decimal GetDiscountPercentage()
    {
        if (DiscountPrice == null || Price.Amount == 0)
            return 0;

        return ((Price.Amount - DiscountPrice.Amount) / Price.Amount) * 100;
    }
}
