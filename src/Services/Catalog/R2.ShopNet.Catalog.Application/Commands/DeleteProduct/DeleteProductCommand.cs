using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands.DeleteProduct;

/// <summary>
/// Command to delete a product (soft delete).
/// </summary>
public record DeleteProductCommand(Guid ProductId) : ICommand<Result>;
