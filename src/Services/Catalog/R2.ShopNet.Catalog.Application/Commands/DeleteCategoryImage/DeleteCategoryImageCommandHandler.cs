using Microsoft.Extensions.Logging;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Commands.DeleteCategoryImage;

/// <summary>
/// Handler for deleting category images from MinIO and database
/// </summary>
[GenerateHandler]
public class DeleteCategoryImageCommandHandler : ICommandHandler<DeleteCategoryImageCommand, Result<DeleteCategoryImageResponse>>
{
    private readonly IMinIORepository<Category> _imageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCategoryImageCommandHandler> _logger;

    public DeleteCategoryImageCommandHandler(
        IMinIORepository<Category> imageRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCategoryImageCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DeleteCategoryImageResponse>> Handle(
        DeleteCategoryImageCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Deleting category image for category {CategoryId}",
            request.CategoryId);

        try
        {
            // Verify category exists
            var categoryRepository = _unitOfWork.Repository<Category>();
            var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
            {
                return Result.Failure<DeleteCategoryImageResponse>(
                    Error.NotFound("Category.NotFound", "Category not found"));
            }

            // Delete all images for this category
            await _imageRepository.DeleteAllFilesAsync(request.CategoryId, cancellationToken);

            // Clear the image URL from the category
            category.SetImageUrl(null);
            await categoryRepository.UpdateAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully deleted images for category {CategoryId}",
                request.CategoryId);

            var response = new DeleteCategoryImageResponse
            {
                Success = true,
                Message = "Category image deleted successfully"
            };
            return Result<DeleteCategoryImageResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete category image for category {CategoryId}",
                request.CategoryId);

            return Result.Failure<DeleteCategoryImageResponse>(
                Error.Failure("DeleteImage.Failed", $"Failed to delete category image: {ex.Message}"));
        }
    }
}
