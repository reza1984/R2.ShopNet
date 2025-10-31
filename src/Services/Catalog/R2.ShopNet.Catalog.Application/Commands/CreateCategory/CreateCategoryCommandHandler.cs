using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.Events;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands.CreateCategory;

/// <summary>
/// Handler for creating a new category.
/// </summary>
[GenerateHandler]
public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<CategoryDto>> Handle(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<CategoryDto>(
                Error.Validation("Name.Empty", "Category name cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(command.Slug))
        {
            return Result.Failure<CategoryDto>(
                Error.Validation("Slug.Empty", "Category slug cannot be empty"));
        }

        var categoryRepository = _unitOfWork.Repository<Category>();

        // Check if slug already exists
        var existingCategory = (await categoryRepository.FindAsync(
            c => c.Slug == command.Slug.ToLowerInvariant(),
            cancellationToken)).FirstOrDefault();

        if (existingCategory != null)
        {
            return Result.Failure<CategoryDto>(
                Error.Conflict("Slug.AlreadyExists", "A category with this slug already exists"));
        }

        // If parent category is specified, validate it exists
        if (command.ParentCategoryId.HasValue)
        {
            var parentCategory = await categoryRepository.GetByIdAsync(
                command.ParentCategoryId.Value,
                cancellationToken);

            if (parentCategory == null)
            {
                return Result.Failure<CategoryDto>(
                    Error.NotFound("ParentCategory.NotFound", "Parent category not found"));
            }
        }

        // Create category
        var category = new Category(
            command.Name,
            command.Slug,
            command.Description,
            command.ParentCategoryId);

        category.SetDisplayOrder(command.DisplayOrder);
        category.SetImageUrl(command.ImageUrl);

        // Add to repository
        await categoryRepository.AddAsync(category, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _eventPublisher.Publish(
            new CategoryCreatedEvent(
                category.Id,
                category.Name,
                category.Slug,
                category.ParentCategoryId),
            cancellationToken);

        // Map to DTO
        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Slug = category.Slug,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = null, // Will need to query separately if needed
            DisplayOrder = category.DisplayOrder,
            ImageUrl = category.ImageUrl,
            ProductCount = 0,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return Result<CategoryDto>.Success(categoryDto);
    }
}
