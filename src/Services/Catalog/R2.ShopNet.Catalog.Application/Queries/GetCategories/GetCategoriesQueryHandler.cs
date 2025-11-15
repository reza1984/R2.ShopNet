using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Queries;

/// <summary>
/// Handler for GetCategoriesQuery to retrieve paginated list of categories.
/// </summary>
[GenerateHandler]
public sealed class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, Result<PagedResult<CategoryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoriesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<CategoryDto>>> Handle(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get read-only repository and start with base query including related entities
            var repository = _unitOfWork.ReadOnlyRepository<Category>();
            var queryable = repository.AsQueryable()
                .Include(c => c.ParentCategory)
                .Include(c => c.Products)
                .AsNoTracking()
                .Where(c => !c.IsDeleted);

            // Apply parent category filter
            if (query.ParentCategoryId.HasValue)
            {
                queryable = queryable.Where(c => c.ParentCategoryId == query.ParentCategoryId.Value);
            }
            else if (query.ParentCategoryId == Guid.Empty)
            {
                // Explicitly filter for root categories (no parent)
                queryable = queryable.Where(c => c.ParentCategoryId == null);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                queryable = queryable.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Description != null && c.Description.ToLower().Contains(searchTerm)) ||
                    c.Slug.ToLower().Contains(searchTerm));
            }

            // Apply sorting
            queryable = query.SortBy?.ToLower() switch
            {
                "name" => query.SortDescending
                    ? queryable.OrderByDescending(c => c.Name)
                    : queryable.OrderBy(c => c.Name),
                "displayorder" => query.SortDescending
                    ? queryable.OrderByDescending(c => c.DisplayOrder)
                    : queryable.OrderBy(c => c.DisplayOrder),
                "createdat" => query.SortDescending
                    ? queryable.OrderByDescending(c => c.CreatedAt)
                    : queryable.OrderBy(c => c.CreatedAt),
                _ => query.SortDescending
                    ? queryable.OrderByDescending(c => c.DisplayOrder)
                    : queryable.OrderBy(c => c.DisplayOrder)
            };

            // Get total count before pagination
            var totalCount = await queryable.CountAsync(cancellationToken);

            // Apply pagination
            var categories = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Slug = c.Slug,
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                    DisplayOrder = c.DisplayOrder,
                    ImageUrl = c.ImageUrl,
                    ProductCount = c.Products.Count(p => !p.IsDeleted),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<CategoryDto>(
                categories,
                query.PageNumber,
                query.PageSize,
                totalCount
                );

            return Result.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return Error.Failure("Categories.RetrievalFailed", $"Failed to retrieve categories: {ex.Message}");
        }
    }
}
