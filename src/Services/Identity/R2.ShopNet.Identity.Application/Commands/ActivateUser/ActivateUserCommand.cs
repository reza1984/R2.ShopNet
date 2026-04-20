using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Command to activate a user account.
/// </summary>
public record ActivateUserCommand(Guid UserId) : ICommand<Result<bool>>;
