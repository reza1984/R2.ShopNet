using R2.ShopNet.Catalog.Application.DTOs;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.Common;

namespace R2.ShopNet.Catalog.Application.Queries.GetCategoryHierarchy;

/// <summary>
/// Query to retrieve the full category hierarchy (tree structure).
/// </summary>
public record GetCategoryHierarchyQuery : IQuery<Result<IReadOnlyList<CategoryHierarchyDto>>>;
