using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands.CreateCategory;

/// <summary>
/// Command to create a new category.
/// </summary>
public record CreateCategoryCommand(
    string Name,
    string Slug,
    string? Description = null,
    Guid? ParentCategoryId = null,
    int DisplayOrder = 0,
    string? ImageUrl = null) : ICommand<Result<CategoryDto>>;
