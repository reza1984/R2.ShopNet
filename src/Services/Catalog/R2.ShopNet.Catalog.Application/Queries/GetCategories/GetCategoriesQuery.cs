using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Application.Queries.GetCategories;

/// <summary>
/// Query to retrieve a paginated list of categories with optional filtering.
/// </summary>
public record GetCategoriesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? ParentCategoryId = null,
    string? SearchTerm = null,
    string? SortBy = "DisplayOrder",
    bool SortDescending = false
) : IQuery<Result<PagedResult<CategoryDto>>>;
