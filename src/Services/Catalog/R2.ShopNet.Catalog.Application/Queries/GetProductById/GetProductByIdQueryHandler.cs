using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.UnitOfWork;
using R2.ShopNet.Framework.Persistence.Storage.Abstractions;

namespace R2.ShopNet.Catalog.Application.Queries.GetProductById;

/// <summary>
/// Handler for GetProductByIdQuery to retrieve a single product by its ID.
/// </summary>
[GenerateHandler]
public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMinIORepository<Product> _imageRepository;

    public GetProductByIdQueryHandler(IUnitOfWork unitOfWork, IMinIORepository<Product> imageRepository)
    {
        _unitOfWork = unitOfWork;
        _imageRepository = imageRepository;
    }

    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.ReadOnlyRepository<Product>();

            // First, get the product without images
            var productData = await repository.AsQueryable()
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsNoTracking()
                .Where(p => p.Id == query.ProductId && !p.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (productData == null)
            {
                return Error.NotFound("Product.NotFound", $"Product with ID '{query.ProductId}' not found");
            }

            // Get images with presigned URLs from MinIO
            var imageMetadata = await _imageRepository.GetFilesWithUrlsAsync(
                query.ProductId,
                expiryMinutes: 60, // URLs valid for 60 minutes
                cancellationToken);

            // Build the DTO with images
            var product = new ProductDto
            {
                Id = productData.Id,
                Name = productData.Name,
                Description = productData.Description,
                ShortDescription = productData.ShortDescription,
                Slug = productData.Slug,
                Sku = productData.Sku,
                Price = productData.Price.Amount,
                Currency = productData.Price.Currency,
                DiscountPrice = productData.DiscountPrice?.Amount,
                DiscountPercentage = productData.DiscountPrice != null
                    ? ((productData.Price.Amount - productData.DiscountPrice.Amount) / productData.Price.Amount) * 100
                    : null,
                StockQuantity = productData.StockQuantity,
                ReorderLevel = productData.ReorderLevel,
                Status = productData.Status.ToString(),
                CategoryId = productData.CategoryId,
                CategoryName = productData.Category.Name,
                Brand = productData.Brand,
                Weight = productData.Weight,
                Dimensions = productData.Dimensions,
                Images = imageMetadata
                    .OrderBy(m => m.DisplayOrder ?? 0)
                    .Select(m => new ProductImageDto
                    {
                        Id = m.Id,
                        Url = m.Url,
                        FileName = m.FileName,
                        ContentType = m.ContentType,
                        SizeInBytes = m.SizeInBytes,
                        AltText = m.Metadata.GetValueOrDefault("altText"),
                        DisplayOrder = m.DisplayOrder ?? 0,
                        IsPrimary = bool.TryParse(m.Metadata.GetValueOrDefault("isPrimary"), out var isPrimary) && isPrimary
                    }).ToList(),
                Variants = productData.Variants
                    .Where(v => v.IsActive)
                    .Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Name = v.Name,
                        Sku = v.Sku,
                        Price = v.Price?.Amount,
                        Currency = v.Price?.Currency,
                        StockQuantity = v.StockQuantity,
                        Weight = v.Weight,
                        Attributes = v.Attributes,
                        ImageUrl = v.ImageUrl,
                        IsActive = v.IsActive
                    }).ToList(),
                MetaTitle = productData.MetaTitle,
                MetaDescription = productData.MetaDescription,
                MetaKeywords = productData.MetaKeywords,
                ViewCount = productData.ViewCount,
                AverageRating = productData.AverageRating,
                ReviewCount = productData.ReviewCount,
                CreatedAt = productData.CreatedAt,
                UpdatedAt = productData.UpdatedAt
            };

            // Optionally increment view count (would need to be done in a separate command)
            // This is just for reading, so we don't modify the entity here

            return Result<ProductDto>.Success(product);
        }
        catch (Exception ex)
        {
            return Error.Failure("Product.RetrievalFailed", $"Failed to retrieve product: {ex.Message}");
        }
    }
}
