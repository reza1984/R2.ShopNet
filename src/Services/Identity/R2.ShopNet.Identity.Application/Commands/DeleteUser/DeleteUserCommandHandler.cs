using Microsoft.AspNetCore.Identity;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Commands.DeleteUser;

/// <summary>
/// Handler for soft deleting a user.
/// </summary>
public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, Result<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public DeleteUserCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<bool>> Handle(
        DeleteUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId.ToString());

        if (user == null || user.IsDeleted)
        {
            return Result.Failure<bool>(
                Error.NotFound("User.NotFound", $"User with ID {command.UserId} not found"));
        }

        // Soft delete
        user.MarkAsDeleted();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<bool>(
                Error.Validation("User.DeleteFailed", $"Failed to delete user: {errors}"));
        }

        return Result.Success(true);
    }
}
