using Microsoft.AspNetCore.Identity;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Identity.Application.DTOs;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Queries;

/// <summary>
/// Handler for getting a user by ID.
/// </summary>
[GenerateHandler]

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUserByIdQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserDto>> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(query.UserId.ToString());

        if (user == null || user.IsDeleted)
        {
            return Result.Failure<UserDto>(
                Error.NotFound("User.NotFound", $"User with ID {query.UserId} not found"));
        }

        var roles = await _userManager.GetRolesAsync(user);

        var userDto = new UserDto(
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
            roles);

        return Result.Success(userDto);
    }
}
