using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Catalog.Domain.Entities;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Persistence.UnitOfWork;

namespace R2.ShopNet.Catalog.Application.Queries.GetCategoryById;

/// <summary>
/// Handler for GetCategoryByIdQuery to retrieve a single category by ID.
/// </summary>
[GenerateHandler]
public sealed class GetCategoryByIdQueryHandler : IQueryHandler<GetCategoryByIdQuery, Result<CategoryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCategoryByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CategoryDto>> Handle(
        GetCategoryByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var repository = _unitOfWork.ReadOnlyRepository<Category>();

            var category = await repository.AsQueryable()
                .Include(c => c.ParentCategory)
                .Include(c => c.Products)
                .AsNoTracking()
                .Where(c => c.Id == query.CategoryId && !c.IsDeleted)
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
                .FirstOrDefaultAsync(cancellationToken);

            if (category == null)
            {
                return Error.NotFound(
                    "Category.NotFound",
                    $"Category with ID '{query.CategoryId}' was not found.");
            }

            return Result<CategoryDto>.Success(category);
        }
        catch (Exception ex)
        {
            return Error.Failure(
                "Category.RetrievalFailed",
                $"Failed to retrieve category: {ex.Message}");
        }
    }
}
