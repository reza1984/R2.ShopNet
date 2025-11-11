using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Domain.Entities;

/// <summary>
/// Represents an image file for a category, stored in MinIO.
/// </summary>
public class CategoryImage : FileEntity
{
    /// <summary>
    /// The category this image belongs to.
    /// </summary>
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Navigation property to the category.
    /// </summary>
    public Category Category { get; private set; } = null!;

    /// <summary>
    /// Alternative text for accessibility.
    /// </summary>
    public string? AltText { get; private set; }

    /// <summary>
    /// Constructor for EF Core.
    /// </summary>
    private CategoryImage() { }

    /// <summary>
    /// Create a new category image.
    /// </summary>
    public CategoryImage(
        Guid categoryId,
        string objectKey,
        string fileName,
        string contentType,
        long sizeInBytes,
        string? altText = null)
    {
        CategoryId = categoryId;
        AltText = altText;
        SetFileMetadata(objectKey, fileName, contentType, sizeInBytes);
    }

    /// <summary>
    /// Update the metadata for this image.
    /// </summary>
    public void UpdateMetadata(string? altText)
    {
        AltText = altText;
    }
}
