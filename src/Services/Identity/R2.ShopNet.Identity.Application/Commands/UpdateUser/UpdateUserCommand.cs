using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Command to update user information.
/// </summary>
public record UpdateUserCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber) : ICommand<Result<bool>>;
