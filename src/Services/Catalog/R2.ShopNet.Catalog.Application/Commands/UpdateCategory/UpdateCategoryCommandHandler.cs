using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Application.Helpers;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.Events;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands.UpdateCategory;

/// <summary>
/// Handler for updating an existing category.
/// </summary>
[GenerateHandler]
public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, Result<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMinIORepository<Category> _categoryImageRepository;

    public UpdateCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IMinIORepository<Category> categoryImageRepository)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _categoryImageRepository = categoryImageRepository;
    }

    public async Task<Result<CategoryDto>> Handle(
        UpdateCategoryCommand command,
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

        // Get existing category
        var category = await categoryRepository.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category == null)
        {
            return Result.Failure<CategoryDto>(
                Error.NotFound("Category.NotFound", "Category not found"));
        }

        // Check if slug is being changed and if new slug already exists
        var normalizedSlug = command.Slug.ToLowerInvariant();
        if (category.Slug != normalizedSlug)
        {
            var existingCategory = await categoryRepository.ExistsAsync(
                c => c.Slug == normalizedSlug && c.Id != command.CategoryId,
                cancellationToken);

            if (existingCategory == true)
            {
                return Result.Failure<CategoryDto>(
                    Error.Conflict("Slug.AlreadyExists", "A category with this slug already exists"));
            }
        }

        // If parent category is specified, validate it exists and prevent circular references
        if (command.ParentCategoryId.HasValue)
        {
            if (command.ParentCategoryId.Value == command.CategoryId)
            {
                return Result.Failure<CategoryDto>(
                    Error.Validation("ParentCategory.CircularReference",
                        "A category cannot be its own parent"));
            }

            var parentCategory = await categoryRepository.GetByIdAsync(
                command.ParentCategoryId.Value,
                cancellationToken);

            if (parentCategory == null)
            {
                return Result.Failure<CategoryDto>(
                    Error.NotFound("ParentCategory.NotFound", "Parent category not found"));
            }

            // Check for circular reference (parent's parent chain contains this category)
            var currentParent = parentCategory;
            while (currentParent.ParentCategoryId.HasValue)
            {
                if (currentParent.ParentCategoryId.Value == command.CategoryId)
                {
                    return Result.Failure<CategoryDto>(
                        Error.Validation("ParentCategory.CircularReference",
                            "Cannot set parent category as it would create a circular reference"));
                }
                currentParent = await categoryRepository.GetByIdAsync(
                    currentParent.ParentCategoryId.Value,
                    cancellationToken);
                if (currentParent == null) break;
            }
        }

        // Update category properties
        category.SetName(command.Name);
        category.SetSlug(command.Slug);
        category.SetDescription(command.Description);
        category.SetParentCategory(command.ParentCategoryId);
        category.SetDisplayOrder(command.DisplayOrder);

        // Upload new image if provided
        if (command.Image != null)
        {
            try
            {
                // Delete old images for this category
                await _categoryImageRepository.DeleteAllFilesAsync(category.Id, cancellationToken);

                // Convert ImageUploadDto to a stream for upload
                using var stream = new MemoryStream(command.Image.FileData);
                stream.Position = 0; // Reset stream position to beginning
                var formFile = new FormFileAdapter(
                    stream,
                    command.Image.FileName,
                    command.Image.ContentType,
                    command.Image.SizeInBytes);

                var fileMetadata = await _categoryImageRepository.UploadFileAsync(
                    category.Id,
                    formFile,
                    new Dictionary<string, string> { { "altText", command.Name } },
                    cancellationToken);

                // Get presigned URL for the uploaded image
                // MinIO max expiry is 604800 seconds (7 days) = 10080 minutes
                var imageUrl = await _categoryImageRepository.GetDownloadUrlAsync(
                    fileMetadata.Id,
                    expiryMinutes: 10080, // 7 days (MinIO's maximum)
                    cancellationToken);

                category.SetImageUrl(imageUrl);
            }
            catch (Exception)
            {
                // Log error but don't fail the category update
                // The category will be updated without changing the image
            }
        }

        // Update in repository
        await categoryRepository.UpdateAsync(category, cancellationToken);

        // Save changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _eventPublisher.Publish(
            new CategoryUpdatedEvent(
                category.Id,
                category.Name,
                category.Slug),
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
            ProductCount = 0, // Not loading products for update response
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return Result<CategoryDto>.Success(categoryDto);
    }
}
