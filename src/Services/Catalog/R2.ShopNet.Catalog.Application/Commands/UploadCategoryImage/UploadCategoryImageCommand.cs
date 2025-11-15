using Microsoft.AspNetCore.Http;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands;

/// <summary>
/// Command to upload a category image to MinIO storage
/// </summary>
public record UploadCategoryImageCommand : ICommand<Result<UploadCategoryImageResponse>>
{
    /// <summary>
    /// Category ID to associate the image with
    /// </summary>
    public required Guid CategoryId { get; init; }

    /// <summary>
    /// The image file to upload
    /// </summary>
    public required IFormFile File { get; init; }

    /// <summary>
    /// Alternative text for accessibility
    /// </summary>
    public string? AltText { get; init; }
}

/// <summary>
/// Response after uploading a category image
/// </summary>
public record UploadCategoryImageResponse
{
    /// <summary>
    /// ID of the created category image record
    /// </summary>
    public required Guid ImageId { get; init; }

    /// <summary>
    /// Original filename
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public required long SizeInBytes { get; init; }

    /// <summary>
    /// Content type
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public required DateTime UploadedAt { get; init; }

    /// <summary>
    /// URL to access the image
    /// </summary>
    public string? ImageUrl { get; init; }
}
