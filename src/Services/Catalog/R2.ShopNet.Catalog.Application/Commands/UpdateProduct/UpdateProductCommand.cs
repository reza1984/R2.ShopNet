using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands.UpdateProduct;

/// <summary>
/// Command to update an existing product.
/// </summary>
public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Slug,
    string Sku,
    decimal Price,
    string Currency,
    Guid CategoryId,
    string? Description = null,
    string? ShortDescription = null,
    decimal? DiscountPrice = null,
    decimal? CostPrice = null,
    int StockQuantity = 0,
    int ReorderLevel = 10,
    string Status = "Draft",
    string? Brand = null,
    decimal? Weight = null,
    string? Dimensions = null,
    string? MetaTitle = null,
    string? MetaDescription = null,
    string? MetaKeywords = null) : ICommand<Result<ProductDto>>;
