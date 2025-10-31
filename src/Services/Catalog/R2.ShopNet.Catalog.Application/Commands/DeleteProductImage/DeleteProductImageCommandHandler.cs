using Microsoft.Extensions.Logging;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;

namespace R2.ShopNet.Catalog.Application.Commands.DeleteProductImage;

/// <summary>
/// Handler for deleting product images from MinIO and database
/// </summary>
public class DeleteProductImageCommandHandler : ICommandHandler<DeleteProductImageCommand, Result<DeleteProductImageResponse>>
{
    private readonly IMinIORepository<Product> _imageRepository;
    private readonly ILogger<DeleteProductImageCommandHandler> _logger;

    public DeleteProductImageCommandHandler(
        IMinIORepository<Product> imageRepository,
        ILogger<DeleteProductImageCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _logger = logger;
    }

    public async Task<Result<DeleteProductImageResponse>> Handle(
        DeleteProductImageCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Deleting product image {ImageId}",
            request.ImageId);

        try
        {
            var deleted = await _imageRepository.DeleteFileAsync(
                request.ImageId,
                cancellationToken);

            if (deleted)
            {
                _logger.LogInformation(
                    "Successfully deleted product image {ImageId}",
                    request.ImageId);

                var response = new DeleteProductImageResponse
                {
                    Success = true,
                    Message = "Product image deleted successfully"
                };
                return Result<DeleteProductImageResponse>.Success(response);
            }
            else
            {
                _logger.LogWarning(
                    "Product image {ImageId} not found",
                    request.ImageId);

                var response = new DeleteProductImageResponse
                {
                    Success = false,
                    Message = "Product image not found"
                };
                return Result<DeleteProductImageResponse>.Success(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete product image {ImageId}",
                request.ImageId);

            return Result.Failure<DeleteProductImageResponse>(
                Error.Failure("DeleteImage.Failed", $"Failed to delete product image: {ex.Message}"));
        }
    }
}
