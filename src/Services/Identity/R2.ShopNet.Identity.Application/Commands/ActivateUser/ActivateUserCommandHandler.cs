using Microsoft.AspNetCore.Identity;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Commands.ActivateUser;

/// <summary>
/// Handler for activating a user account.
/// </summary>
public class ActivateUserCommandHandler : ICommandHandler<ActivateUserCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivateUserCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(
        ActivateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId.ToString());

        if (user == null || user.IsDeleted)
        {
            return Result.Failure<bool>(
                Error.NotFound("User.NotFound", $"User with ID {command.UserId} not found"));
        }

        user.Activate();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<bool>(
                Error.Validation("User.ActivateFailed", $"Failed to activate user: {errors}"));
        }

        return Result.Success(true);
    }
}
