using Microsoft.AspNetCore.Identity;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Handler for deactivating a user account.
/// </summary>
[GenerateHandler]

public class DeactivateUserCommandHandler : ICommandHandler<DeactivateUserCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DeactivateUserCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(
        DeactivateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId.ToString());

        if (user == null || user.IsDeleted)
        {
            return Result.Failure<bool>(
                Error.NotFound("User.NotFound", $"User with ID {command.UserId} not found"));
        }

        user.Deactivate();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<bool>(
                Error.Validation("User.DeactivateFailed", $"Failed to deactivate user: {errors}"));
        }

        return Result.Success(true);
    }
}
