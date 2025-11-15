using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Queries;

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
