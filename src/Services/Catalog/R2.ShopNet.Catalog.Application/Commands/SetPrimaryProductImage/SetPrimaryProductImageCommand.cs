using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands;

/// <summary>
/// Command to set a product image as the primary image.
/// </summary>
public record SetPrimaryProductImageCommand(
    Guid ProductId,
    Guid ImageId) : ICommand<Result<bool>>;
