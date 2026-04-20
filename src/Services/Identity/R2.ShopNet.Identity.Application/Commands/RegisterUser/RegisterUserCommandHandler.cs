using Microsoft.AspNetCore.Identity;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Framework.Events;
using R2.ShopNet.Identity.Domain.Entities;
using R2.ShopNet.Identity.Domain.Events;

namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Handler for user registration command using ASP.NET Core Identity.
/// </summary>
[GenerateHandler]
public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEventPublisher _eventPublisher;

    public RegisterUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEventPublisher eventPublisher)
    {
        _userManager = userManager;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        // Validate email format
        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return Result.Failure<RegisterUserResponse>(
                Error.Validation("Email.Invalid", "Invalid email format"));
        }

        // Validate password (basic validation, Identity will do additional validation)
        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 8)
        {
            return Result.Failure<RegisterUserResponse>(
                Error.Validation("Password.TooShort", "Password must be at least 8 characters"));
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser != null)
        {
            return Result.Failure<RegisterUserResponse>(
                Error.Conflict("Email.AlreadyExists", "A user with this email already exists"));
        }

        // Create the user using ApplicationUser
        var user = new ApplicationUser(
            email: command.Email,
            firstName: command.FirstName,
            lastName: command.LastName)
        {
            PhoneNumber = command.PhoneNumber,
            CreatedBy = "System"
        };

        // Create user with password (UserManager handles hashing)
        var result = await _userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure<RegisterUserResponse>(
                Error.Validation("User.CreationFailed", $"Failed to create user: {errors}"));
        }

        // Add user to default "User" role
        var roleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            // Log warning but don't fail - user was created successfully
            // TODO: Add logging
        }

        // Publish domain event
        await _eventPublisher.Publish(
            new UserRegisteredEvent(user.Id, user.Email!, user.FirstName, user.LastName),
            cancellationToken);

        // Return response
        return Result.Success(new RegisterUserResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
