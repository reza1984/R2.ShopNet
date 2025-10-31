using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.Events;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands.DeleteCategory;

/// <summary>
/// Handler for deleting a category (soft delete).
/// </summary>
[GenerateHandler]
public class DeleteCategoryCommandHandler : ICommandHandler<DeleteCategoryCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public DeleteCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result> Handle(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var categoryRepository = _unitOfWork.Repository<Category>();

        // Get existing category
        var category = await categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result.Failure(
                Error.NotFound("Category.NotFound", "Category not found"));
        }

        // Check if category has products
        var productRepository = _unitOfWork.Repository<Product>();
        var hasProducts = (await productRepository.FindAsync(
            p => p.CategoryId == command.CategoryId && !p.IsDeleted,
            cancellationToken)).Any();

        if (hasProducts)
        {
            return Result.Failure(
                Error.Conflict("Category.HasProducts",
                    "Cannot delete category that contains products"));
        }

        // Check if category has subcategories
        var hasSubCategories = (await categoryRepository.FindAsync(
            c => c.ParentCategoryId == command.CategoryId && !c.IsDeleted,
            cancellationToken)).Any();

        if (hasSubCategories)
        {
            return Result.Failure(
                Error.Conflict("Category.HasSubCategories",
                    "Cannot delete category that has subcategories"));
        }

        // Perform soft delete
        await categoryRepository.SoftDeleteAsync(command.CategoryId, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _eventPublisher.Publish(
            new CategoryDeletedEvent(
                category.Id,
                category.Name),
            cancellationToken);

        return Result.Success();
    }
}
