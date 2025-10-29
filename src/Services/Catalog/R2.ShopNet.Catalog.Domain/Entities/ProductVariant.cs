using R2.ShopNet.Catalog.Domain.ValueObjects;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Domain.Entities;

/// <summary>
/// Represents a product variant (e.g., different size, color, material).
/// </summary>
public class ProductVariant : BaseEntity
{
    /// <summary>
    /// The product this variant belongs to.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Variant name (e.g., "Red - Large").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Variant SKU (unique identifier).
    /// </summary>
    public string Sku { get; private set; } = string.Empty;

    /// <summary>
    /// Variant-specific price (if different from base product).
    /// </summary>
    public Money? Price { get; private set; }

    /// <summary>
    /// Variant-specific stock quantity.
    /// </summary>
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Variant-specific weight.
    /// </summary>
    public decimal? Weight { get; private set; }

    /// <summary>
    /// Variant attribute values (e.g., "Color:Red,Size:Large").
    /// </summary>
    public Dictionary<string, string> Attributes { get; private set; } = new();

    /// <summary>
    /// Image URL specific to this variant.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Whether this variant is active.
    /// </summary>
    public bool IsActive { get; private set; }

    private ProductVariant() { }

    public ProductVariant(Guid productId, string name, string sku, int stockQuantity = 0)
    {
        ProductId = productId;
        SetName(name);
        SetSku(sku);
        StockQuantity = stockQuantity;
        IsActive = true;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variant name cannot be empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Variant name cannot exceed 100 characters", nameof(name));

        Name = name;
    }

    public void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("Variant SKU cannot be empty", nameof(sku));

        if (sku.Length > 50)
            throw new ArgumentException("Variant SKU cannot exceed 50 characters", nameof(sku));

        Sku = sku.ToUpperInvariant();
    }

    public void SetPrice(Money? price)
    {
        Price = price;
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(quantity));

        StockQuantity = quantity;
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

    public void SetWeight(decimal? weight)
    {
        if (weight < 0)
            throw new ArgumentException("Weight cannot be negative", nameof(weight));

        Weight = weight;
    }

    public void SetImageUrl(string? imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public void SetAttribute(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Attribute key cannot be empty", nameof(key));

        Attributes[key] = value;
    }

    public void RemoveAttribute(string key)
    {
        Attributes.Remove(key);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
