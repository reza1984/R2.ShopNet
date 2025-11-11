using Microsoft.Extensions.Logging;
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

namespace R2.ShopNet.Catalog.Application.Commands.CreateCategory;

/// <summary>
/// Handler for creating a new category.
/// </summary>
[GenerateHandler]
public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Result<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMinIORepository<Category> _categoryImageRepository;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        IMinIORepository<Category> categoryImageRepository,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _categoryImageRepository = categoryImageRepository;
        _logger = logger;
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
        var normalizedSlug = command.Slug.ToLowerInvariant();
        var existingCategory = (await categoryRepository.FindAsync(
            c => c.Slug == normalizedSlug,
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

        // Add to repository
        await categoryRepository.AddAsync(category, cancellationToken);

        // Save changes to get the category ID
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Upload image if provided
        string? imageUrl = null;
        if (command.Image != null)
        {
            try
            {
                _logger.LogInformation("Starting image upload for category {CategoryId}, FileName: {FileName}, ContentType: {ContentType}, Size: {Size} bytes",
                    category.Id, command.Image.FileName, command.Image.ContentType, command.Image.SizeInBytes);

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

                _logger.LogInformation("Image uploaded successfully for category {CategoryId}, FileId: {FileId}", category.Id, fileMetadata.Id);

                // Get presigned URL for the uploaded image
                // MinIO max expiry is 604800 seconds (7 days) = 10080 minutes
                imageUrl = await _categoryImageRepository.GetDownloadUrlAsync(
                    fileMetadata.Id,
                    expiryMinutes: 10080, // 7 days (MaxIO's maximum)
                    cancellationToken);

                _logger.LogInformation("Generated presigned URL for category {CategoryId}: {ImageUrl}", category.Id, imageUrl);

                category.SetImageUrl(imageUrl);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image for category {CategoryId}: {ErrorMessage}", category.Id, ex.Message);
                // Don't fail the category creation - the category will be created without an image
            }
        }

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
