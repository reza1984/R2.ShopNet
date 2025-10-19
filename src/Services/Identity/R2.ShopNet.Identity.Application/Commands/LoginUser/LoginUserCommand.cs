using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.LoginUser;

/// <summary>
/// Command to authenticate a user and generate tokens.
/// </summary>
public record LoginUserCommand(
    string Email,
    string Password) : ICommand<Result<LoginUserResponse>>;
