using Microsoft.Extensions.Logging;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands;

/// <summary>
/// Handler for uploading category images to MinIO
/// </summary>
[GenerateHandler]
public class UploadCategoryImageCommandHandler : ICommandHandler<UploadCategoryImageCommand, Result<UploadCategoryImageResponse>>
{
    private readonly IMinIORepository<Category> _imageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadCategoryImageCommandHandler> _logger;

    public UploadCategoryImageCommandHandler(
        IMinIORepository<Category> imageRepository,
        IUnitOfWork unitOfWork,
        ILogger<UploadCategoryImageCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UploadCategoryImageResponse>> Handle(
        UploadCategoryImageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Verify category exists
            var categoryRepository = _unitOfWork.Repository<Category>();
            var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
            {
                return Result.Failure<UploadCategoryImageResponse>(
                    Error.NotFound("Category.NotFound", "Category not found"));
            }

            _logger.LogInformation(
                "Uploading image for category {CategoryId}: {FileName} ({Size} bytes)",
                request.CategoryId,
                request.File.FileName,
                request.File.Length);

            // Delete old images for this category
            await _imageRepository.DeleteAllFilesAsync(request.CategoryId, cancellationToken);

            // Build metadata dictionary
            var metadata = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(request.AltText))
            {
                metadata["altText"] = request.AltText;
            }

            // Upload file through repository
            var fileMetadata = await _imageRepository.UploadFileAsync(
                request.CategoryId,
                request.File,
                metadata,
                cancellationToken);

            // Get presigned URL for the uploaded image
            // MinIO max expiry is 604800 seconds (7 days) = 10080 minutes
            var imageUrl = await _imageRepository.GetDownloadUrlAsync(
                fileMetadata.Id,
                expiryMinutes: 10080, // 7 days (MinIO's maximum)
                cancellationToken);

            // Update category with new image URL
            category.SetImageUrl(imageUrl);
            await categoryRepository.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded image {ImageId} for category {CategoryId}",
                fileMetadata.Id,
                request.CategoryId);

            // Map to response
            var response = new UploadCategoryImageResponse
            {
                ImageId = fileMetadata.Id,
                FileName = fileMetadata.FileName,
                SizeInBytes = fileMetadata.SizeInBytes,
                ContentType = fileMetadata.ContentType,
                UploadedAt = fileMetadata.UploadedAt,
                ImageUrl = imageUrl
            };

            return Result<UploadCategoryImageResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image for category {CategoryId}", request.CategoryId);
            return Result.Failure<UploadCategoryImageResponse>(
                Error.Failure("UploadImage.Failed", ex.Message));
        }
    }
}
