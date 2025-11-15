using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Queries;

/// <summary>
/// Query to get paginated list of products.
/// </summary>
public record GetProductsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? CategoryId = null,
    string? SearchTerm = null,
    string? Status = null,
    string? SortBy = "CreatedAt",
    bool SortDescending = true) : IQuery<Result<PagedResult<ProductDto>>>;
