using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.RegisterUser;

/// <summary>
/// Command to register a new user.
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : ICommand<Result<RegisterUserResponse>>;
