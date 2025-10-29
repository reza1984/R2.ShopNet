using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Domain.Entities;

/// <summary>
/// Represents a product image.
/// </summary>
public class ProductImage : BaseEntity
{
    /// <summary>
    /// The product this image belongs to.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// URL to the image.
    /// </summary>
    public string ImageUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Alternative text for accessibility.
    /// </summary>
    public string? AltText { get; private set; }

    /// <summary>
    /// Display order for sorting images.
    /// </summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// Whether this is the primary/main image.
    /// </summary>
    public bool IsPrimary { get; private set; }

    private ProductImage() { }

    public ProductImage(Guid productId, string imageUrl, string? altText = null, int displayOrder = 0, bool isPrimary = false)
    {
        ProductId = productId;
        SetImageUrl(imageUrl);
        AltText = altText;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }

    public void SetImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL cannot be empty", nameof(imageUrl));

        ImageUrl = imageUrl;
    }

    public void SetAltText(string? altText)
    {
        AltText = altText;
    }

    public void SetDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void UnsetAsPrimary()
    {
        IsPrimary = false;
    }
}
