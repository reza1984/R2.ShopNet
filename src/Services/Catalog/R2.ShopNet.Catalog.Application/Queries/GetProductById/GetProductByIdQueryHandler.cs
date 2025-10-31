using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Queries.GetProductById;

/// <summary>
/// Handler for GetProductByIdQuery to retrieve a single product by its ID.
/// </summary>
[GenerateHandler]
public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.ReadOnlyRepository<Product>();
            var product = await repository.AsQueryable()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsNoTracking()
                .Where(p => p.Id == query.ProductId && !p.IsDeleted)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    ShortDescription = p.ShortDescription,
                    Slug = p.Slug,
                    Sku = p.Sku,
                    Price = p.Price.Amount,
                    Currency = p.Price.Currency,
                    DiscountPrice = p.DiscountPrice != null ? p.DiscountPrice.Amount : null,
                    DiscountPercentage = p.DiscountPrice != null
                        ? ((p.Price.Amount - p.DiscountPrice.Amount) / p.Price.Amount) * 100
                        : null,
                    StockQuantity = p.StockQuantity,
                    ReorderLevel = p.ReorderLevel,
                    Status = p.Status.ToString(),
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    Brand = p.Brand,
                    Weight = p.Weight,
                    Dimensions = p.Dimensions,
                    Images = p.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
                    {
                        Id = i.Id,
                        Url = string.Empty, // Will be populated with presigned URL
                        FileName = i.FileName,
                        ContentType = i.ContentType,
                        SizeInBytes = i.SizeInBytes,
                        AltText = i.AltText,
                        DisplayOrder = i.DisplayOrder,
                        IsPrimary = i.IsPrimary
                    }).ToList(),
                    Variants = p.Variants.Where(v => v.IsActive).Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        Name = v.Name,
                        Sku = v.Sku,
                        Price = v.Price != null ? v.Price.Amount : null,
                        Currency = v.Price != null ? v.Price.Currency : null,
                        StockQuantity = v.StockQuantity,
                        Weight = v.Weight,
                        Attributes = v.Attributes,
                        ImageUrl = v.ImageUrl,
                        IsActive = v.IsActive
                    }).ToList(),
                    MetaTitle = p.MetaTitle,
                    MetaDescription = p.MetaDescription,
                    MetaKeywords = p.MetaKeywords,
                    ViewCount = p.ViewCount,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                return Error.NotFound("Product.NotFound", $"Product with ID '{query.ProductId}' not found");
            }

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
