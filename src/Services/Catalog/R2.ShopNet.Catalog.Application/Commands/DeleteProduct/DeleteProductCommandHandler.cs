using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.Events;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Framework.Persistence.Auditing;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands;

/// <summary>
/// Handler for deleting a product (soft delete).
/// </summary>
[GenerateHandler]
public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public DeleteProductCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ICurrentUserAccessor currentUserAccessor)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<Result> Handle(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        var productRepository = _unitOfWork.Repository<Product>();

        // Get existing product
        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product == null)
        {
            return Result.Failure(
                Error.NotFound("Product.NotFound", "Product not found"));
        }

        // Get current user ID for auditing purposes
        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Result.Failure(
                Error.Unauthorized("User.Unauthenticated", "Current user is not authenticated"));
        }

        // Perform soft delete
        var deletedProduct = await productRepository.SoftDeleteAsync(command.ProductId, currentUserId, cancellationToken);

        // Append deleted timestamp to slug and SKU to free them up for future use
        if (deletedProduct is not null)
        {
            var timestamp = DateTime.Now.ToFileTimeUtc();
            deletedProduct.SetSlug(deletedProduct.Slug + $"-deleted-{timestamp}");
            deletedProduct.SetSku(deletedProduct.Sku + $"-DEL{timestamp}");
            await productRepository.UpdateAsync(deletedProduct);
        }

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _eventPublisher.Publish(
            new ProductDeletedEvent(product.Id, product.Name, product.Sku),
            cancellationToken);

        return Result.Success();
    }
}
