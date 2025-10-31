using Microsoft.Extensions.Logging;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;

namespace R2.ShopNet.Catalog.Application.Commands.UploadProductImage;

/// <summary>
/// Handler for uploading product images to MinIO
/// </summary>
public class UploadProductImageCommandHandler : ICommandHandler<UploadProductImageCommand, Result<UploadProductImageResponse>>
{
    private readonly IMinIORepository<Product> _imageRepository;
    private readonly ILogger<UploadProductImageCommandHandler> _logger;

    public UploadProductImageCommandHandler(
        IMinIORepository<Product> imageRepository,
        ILogger<UploadProductImageCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _logger = logger;
    }

    public async Task<Result<UploadProductImageResponse>> Handle(
        UploadProductImageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Uploading image for product {ProductId}: {FileName} ({Size} bytes)",
                request.ProductId,
                request.File.FileName,
                request.File.Length);

            // Build metadata dictionary
            var metadata = new Dictionary<string, string>
            {
                { "displayOrder", request.DisplayOrder.ToString() },
                { "isPrimary", request.IsPrimary.ToString() }
            };

            if (!string.IsNullOrWhiteSpace(request.AltText))
            {
                metadata["altText"] = request.AltText;
            }

            // Upload file through repository
            var fileMetadata = await _imageRepository.UploadFileAsync(
                request.ProductId,
                request.File,
                metadata,
                cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded image {ImageId} for product {ProductId}",
                fileMetadata.Id,
                request.ProductId);

            // Map to response
            var response = new UploadProductImageResponse
            {
                ImageId = fileMetadata.Id,
                FileName = fileMetadata.FileName,
                SizeInBytes = fileMetadata.SizeInBytes,
                ContentType = fileMetadata.ContentType,
                UploadedAt = fileMetadata.UploadedAt,
                DisplayOrder = request.DisplayOrder,
                IsPrimary = request.IsPrimary
            };

            return Result<UploadProductImageResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image for product {ProductId}", request.ProductId);
            return Result.Failure<UploadProductImageResponse>(
                Error.Failure("UploadImage.Failed", ex.Message));
        }
    }
}
