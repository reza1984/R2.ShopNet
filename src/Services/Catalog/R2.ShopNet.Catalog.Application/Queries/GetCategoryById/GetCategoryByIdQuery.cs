using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Application.Queries;

/// <summary>
/// Query to retrieve a single category by its ID.
/// </summary>
public record GetCategoryByIdQuery(Guid CategoryId) : IQuery<Result<CategoryDto>>;
