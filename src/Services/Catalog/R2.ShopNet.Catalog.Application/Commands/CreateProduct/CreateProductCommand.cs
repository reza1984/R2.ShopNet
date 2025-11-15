using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands;

/// <summary>
/// Command to create a new product.
/// </summary>
public record CreateProductCommand(
    string Name,
    string Slug,
    string Sku,
    decimal Price,
    string Currency,
    Guid CategoryId,
    string? Description = null,
    string? ShortDescription = null,
    decimal? DiscountPrice = null,
    int StockQuantity = 0,
    int ReorderLevel = 10,
    string? Brand = null,
    decimal? Weight = null,
    string? Dimensions = null,
    Guid? PrimaryImageId = null) : ICommand<Result<ProductDto>>;
