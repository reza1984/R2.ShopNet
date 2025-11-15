using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands;

/// <summary>
/// Command to delete a product image from MinIO storage and database
/// </summary>
public record DeleteProductImageCommand : ICommand<Result<DeleteProductImageResponse>>
{
    /// <summary>
    /// ID of the product image to delete
    /// </summary>
    public required Guid ImageId { get; init; }
}

/// <summary>
/// Response after deleting a product image
/// </summary>
public record DeleteProductImageResponse
{
    /// <summary>
    /// Whether the deletion was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Optional message (e.g., error message if failed)
    /// </summary>
    public string? Message { get; init; }
}
