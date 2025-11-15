using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Queries;

/// <summary>
/// Query to get a product by its ID.
/// </summary>
public record GetProductByIdQuery(Guid ProductId) : IQuery<Result<ProductDto>>;
