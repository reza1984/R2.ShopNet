using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Domain.Entities;

/// <summary>
/// Represents a product image stored in MinIO.
/// </summary>
public class ProductImage : FileEntity
{
    /// <summary>
    /// The product this image belongs to.
    /// </summary>
    public Guid ProductId { get; private set; }
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

    private ProductImage()
    {
    }

    /// <summary>
    /// Creates a new product image with file metadata.
    /// </summary>
    public ProductImage(
        Guid productId,
        string objectKey,
        string fileName,
        string contentType,
        long sizeInBytes,
        string? altText = null,
        int displayOrder = 0,
        bool isPrimary = false)
    {
        ProductId = productId;
        SetFileMetadata(objectKey, fileName, contentType, sizeInBytes);
        AltText = altText;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
    }

    /// <summary>
    /// Updates the image metadata.
    /// </summary>
    public void UpdateMetadata(string? altText = null, int? displayOrder = null, bool? isPrimary = null)
    {
        if (altText is not null) AltText = altText;
        if (displayOrder is not null) DisplayOrder = displayOrder.Value;
        if (isPrimary is not null) IsPrimary = isPrimary.Value;
    }

    /// <summary>
    /// Marks this image as primary.
    /// </summary>
    public void MarkAsPrimary() => IsPrimary = true;

    /// <summary>
    /// Unmarks this image as primary.
    /// </summary>
    public void UnmarkAsPrimary() => IsPrimary = false;
}
