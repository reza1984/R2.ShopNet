using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.DTOs;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Queries.GetUsers;

/// <summary>
/// Handler for getting paginated list of users.
/// </summary>
public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, Result<PagedResult<UserDto>>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUsersQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<PagedResult<UserDto>>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        var usersQuery = _userManager.Users.Where(u => !u.IsDeleted);

        // Apply filters
        if (query.IsActive.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Email!.ToLower().Contains(searchTerm) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)));
        }

        // Get total count
        var totalCount = await usersQuery.CountAsync(cancellationToken);

        // Apply pagination
        var users = await usersQuery
            .OrderByDescending(u => u.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs with roles
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto(
                user.Id,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.IsActive,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.LastLoginAt,
                user.CreatedAt,
                user.UpdatedAt,
                roles));
        }

        var pagedResult = new PagedResult<UserDto>(
            userDtos,
            totalCount,
            query.PageNumber,
            query.PageSize);

        return Result.Success(pagedResult);
    }
}
