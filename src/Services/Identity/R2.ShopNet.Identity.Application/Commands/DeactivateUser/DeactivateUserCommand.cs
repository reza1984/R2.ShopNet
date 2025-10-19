using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.DeactivateUser;

/// <summary>
/// Command to deactivate a user account.
/// </summary>
public record DeactivateUserCommand(Guid UserId) : ICommand<Result<bool>>;
