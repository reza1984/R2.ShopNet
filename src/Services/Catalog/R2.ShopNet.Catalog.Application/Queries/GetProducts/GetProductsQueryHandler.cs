using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Catalog.Domain.Enums;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Queries.GetProducts;

/// <summary>
/// Handler for GetProductsQuery to retrieve paginated list of products.
/// </summary>
/// 
[GenerateHandler]
public sealed class GetProductsQueryHandler : IQueryHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetProductsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<ProductDto>>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get read-only repository and start with base query including related entities
            var repository = _unitOfWork.ReadOnlyRepository<Product>();
            var queryable = repository.AsQueryable()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

            // Apply category filter
            if (query.CategoryId.HasValue)
            {
                queryable = queryable.Where(p => p.CategoryId == query.CategoryId.Value);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                queryable = queryable.Where(p =>
                    p.Name.ToLower().Contains(searchTerm) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                    p.Sku.ToLower().Contains(searchTerm) ||
                    (p.Brand != null && p.Brand.ToLower().Contains(searchTerm)));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<ProductStatus>(query.Status, true, out var status))
            {
                queryable = queryable.Where(p => p.Status == status);
            }

            // Apply sorting
            queryable = query.SortBy?.ToLower() switch
            {
                "name" => query.SortDescending
                    ? queryable.OrderByDescending(p => p.Name)
                    : queryable.OrderBy(p => p.Name),
                "price" => query.SortDescending
                    ? queryable.OrderByDescending(p => p.Price.Amount)
                    : queryable.OrderBy(p => p.Price.Amount),
                "stock" => query.SortDescending
                    ? queryable.OrderByDescending(p => p.StockQuantity)
                    : queryable.OrderBy(p => p.StockQuantity),
                "rating" => query.SortDescending
                    ? queryable.OrderByDescending(p => p.AverageRating)
                    : queryable.OrderBy(p => p.AverageRating),
                "createdat" => query.SortDescending
                    ? queryable.OrderByDescending(p => p.CreatedAt)
                    : queryable.OrderBy(p => p.CreatedAt),
                _ => query.SortDescending
                    ? queryable.OrderByDescending(p => p.CreatedAt)
                    : queryable.OrderBy(p => p.CreatedAt)
            };

            // Get total count before pagination
            var totalCount = await queryable.CountAsync(cancellationToken);

            // Apply pagination
            var products = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
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
                    Images = p.Images.Select(i => new ProductImageDto
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
                    Variants = p.Variants.Select(v => new ProductVariantDto
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
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<ProductDto>(
                products,
                totalCount,
                query.PageNumber,
                query.PageSize);

            return Result<PagedResult<ProductDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return Error.Failure("Products.RetrievalFailed", $"Failed to retrieve products: {ex.Message}");
        }
    }
}
