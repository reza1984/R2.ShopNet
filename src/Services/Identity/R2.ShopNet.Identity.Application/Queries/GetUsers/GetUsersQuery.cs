using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.DTOs;

namespace R2.ShopNet.Identity.Application.Queries;

/// <summary>
/// Query to get a paginated list of users.
/// </summary>
public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    bool? IsActive = null) : IQuery<Result<PagedResult<UserDto>>>;
