using Microsoft.AspNetCore.Identity;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Identity.Application.Services;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Handler for user login command using ASP.NET Core Identity.
/// </summary>
[GenerateHandler]
public class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, Result<LoginUserResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;

    public LoginUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginUserResponse>> Handle(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure<LoginUserResponse>(
                Error.Validation("Login.InvalidCredentials", "Invalid email or password"));
        }

        // Get user by email
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user == null)
        {
            return Result.Failure<LoginUserResponse>(
                Error.Validation("Login.InvalidCredentials", "Invalid email or password"));
        }

        // Check if user can login (not locked, active, etc.)
        if (!user.CanLogin())
        {
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                return Result.Failure<LoginUserResponse>(
                    Error.Validation("Login.AccountLocked", "Account is locked. Please try again later."));
            }

            if (!user.IsActive)
            {
                return Result.Failure<LoginUserResponse>(
                    Error.Validation("Login.AccountInactive", "Account is inactive. Please contact support."));
            }
        }

        // Verify password using SignInManager (handles lockout automatically)
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            if (signInResult.IsLockedOut)
            {
                return Result.Failure<LoginUserResponse>(
                    Error.Validation("Login.AccountLocked", "Account is locked due to multiple failed login attempts."));
            }

            if (signInResult.IsNotAllowed)
            {
                return Result.Failure<LoginUserResponse>(
                    Error.Validation("Login.NotAllowed", "Login not allowed. Please confirm your email."));
            }

            return Result.Failure<LoginUserResponse>(
                Error.Validation("Login.InvalidCredentials", "Invalid email or password"));
        }

        // Record successful login
        user.RecordSuccessfulLogin();
        await _userManager.UpdateAsync(user);

        // Generate tokens
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddHours(1); // Token expiration

        // Return response
        return Result.Success(new LoginUserResponse(
            user.Id,
            user.Email!,
            accessToken,
            refreshToken,
            expiresAt));
    }
}
