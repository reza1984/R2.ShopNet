using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands.UpdateCategory;

/// <summary>
/// Command to update an existing category.
/// </summary>
public record UpdateCategoryCommand(
    Guid CategoryId,
    string Name,
    string Slug,
    string? Description = null,
    Guid? ParentCategoryId = null,
    int DisplayOrder = 0,
    string? ImageUrl = null) : ICommand<Result<CategoryDto>>;
