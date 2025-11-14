using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands.DeleteCategoryImage;

/// <summary>
/// Command to delete a category image from MinIO storage and database
/// </summary>
public record DeleteCategoryImageCommand : ICommand<Result<DeleteCategoryImageResponse>>
{
    /// <summary>
    /// ID of the category to delete the image from
    /// </summary>
    public required Guid CategoryId { get; init; }
}

/// <summary>
/// Response after deleting a category image
/// </summary>
public record DeleteCategoryImageResponse
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
