using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Queries;

/// <summary>
/// Handler for GetCategoryHierarchyQuery to retrieve the full category tree structure.
/// </summary>
[GenerateHandler]
public sealed class GetCategoryHierarchyQueryHandler : IQueryHandler<GetCategoryHierarchyQuery, Result<IReadOnlyList<CategoryHierarchyDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoryHierarchyQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<CategoryHierarchyDto>>> Handle(
        GetCategoryHierarchyQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.ReadOnlyRepository<Category>();

            // Load all categories with their products
            var allCategories = await repository.AsQueryable()
                .Include(c => c.Products)
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            // Build the hierarchy
            IReadOnlyList<CategoryHierarchyDto> hierarchy = BuildHierarchy(allCategories, null);

            return Result<IReadOnlyList<CategoryHierarchyDto>>.Success(hierarchy);
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "CategoryHierarchy.RetrievalFailed",
                $"Failed to retrieve category hierarchy: {ex.Message}");
        }
    }

    private static List<CategoryHierarchyDto> BuildHierarchy(
        List<Category> allCategories,
        Guid? parentId)
    {
        return allCategories
            .Where(c => c.ParentCategoryId == parentId)
            .Select(c => new CategoryHierarchyDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Slug = c.Slug,
                ParentCategoryId = c.ParentCategoryId,
                DisplayOrder = c.DisplayOrder,
                ImageUrl = c.ImageUrl,
                ProductCount = c.Products.Count(p => !p.IsDeleted),
                SubCategories = BuildHierarchy(allCategories, c.Id),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToList();
    }
}
