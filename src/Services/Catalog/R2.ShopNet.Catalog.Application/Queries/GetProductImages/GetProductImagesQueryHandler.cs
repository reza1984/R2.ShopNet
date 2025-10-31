using Microsoft.Extensions.Logging;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;

namespace R2.ShopNet.Catalog.Application.Queries.GetProductImages;

/// <summary>
/// Handler for getting product images with presigned URLs
/// </summary>
public class GetProductImagesQueryHandler : IQueryHandler<GetProductImagesQuery, Result<GetProductImagesResponse>>
{
    private readonly IMinIORepository<Product> _imageRepository;
    private readonly ILogger<GetProductImagesQueryHandler> _logger;

    public GetProductImagesQueryHandler(
        IMinIORepository<Product> imageRepository,
        ILogger<GetProductImagesQueryHandler> logger)
    {
        _imageRepository = imageRepository;
        _logger = logger;
    }

    public async Task<Result<GetProductImagesResponse>> Handle(
        GetProductImagesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Getting images for product {ProductId}",
                request.ProductId);

            // Get files with presigned URLs from repository
            var fileMetadataList = await _imageRepository.GetFilesWithUrlsAsync(
                request.ProductId,
                request.ExpiryMinutes,
                cancellationToken);

            // Map to DTOs
            var images = fileMetadataList.Select(fm => new ProductImageDto
            {
                Id = fm.Id,
                Url = fm.Url,
                FileName = fm.FileName,
                ContentType = fm.ContentType,
                SizeInBytes = fm.SizeInBytes,
                AltText = fm.Metadata.GetValueOrDefault("altText"),
                DisplayOrder = fm.DisplayOrder ?? 0,
                IsPrimary = bool.TryParse(fm.Metadata.GetValueOrDefault("isPrimary"), out var isPrimary) && isPrimary,
                UploadedAt = fm.UploadedAt
            }).ToList();

            _logger.LogInformation(
                "Found {Count} images for product {ProductId}",
                images.Count,
                request.ProductId);

            var response = new GetProductImagesResponse
            {
                ProductId = request.ProductId,
                Images = images
            };

            return Result<GetProductImagesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get images for product {ProductId}", request.ProductId);
            return Result.Failure<GetProductImagesResponse>(
                Error.Failure("GetProductImages.Failed", $"Failed to get product images: {ex.Message}"));
        }
    }
}
