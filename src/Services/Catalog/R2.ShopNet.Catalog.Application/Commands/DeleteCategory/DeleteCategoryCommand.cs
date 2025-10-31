using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Catalog.Application.Commands.DeleteCategory;

/// <summary>
/// Command to delete a category (soft delete).
/// </summary>
public record DeleteCategoryCommand(Guid CategoryId) : ICommand<Result>;
