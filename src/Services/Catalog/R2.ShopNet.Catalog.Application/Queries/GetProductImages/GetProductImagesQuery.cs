using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Queries.GetProductImages;

/// <summary>
/// Query to get all images for a product with presigned download URLs
/// </summary>
public record GetProductImagesQuery : IQuery<Result<GetProductImagesResponse>>
{
    /// <summary>
    /// Product ID to get images for
    /// </summary>
    public required Guid ProductId { get; init; }

    /// <summary>
    /// URL expiry time in minutes (default: 60 minutes)
    /// </summary>
    public int ExpiryMinutes { get; init; } = 60;
}

/// <summary>
/// Response containing product images with presigned URLs
/// </summary>
public record GetProductImagesResponse
{
    /// <summary>
    /// Product ID
    /// </summary>
    public required Guid ProductId { get; init; }

    /// <summary>
    /// List of product images
    /// </summary>
    public required List<ProductImageDto> Images { get; init; }
}

/// <summary>
/// DTO for a product image with download URL
/// </summary>
public record ProductImageDto
{
    /// <summary>
    /// Image ID
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Presigned download URL (valid for specified duration)
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Original filename
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Content type (e.g., "image/jpeg")
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public required long SizeInBytes { get; init; }

    /// <summary>
    /// Alternative text for accessibility
    /// </summary>
    public string? AltText { get; init; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; init; }

    /// <summary>
    /// Whether this is the primary image
    /// </summary>
    public bool IsPrimary { get; init; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public required DateTime UploadedAt { get; init; }
}
