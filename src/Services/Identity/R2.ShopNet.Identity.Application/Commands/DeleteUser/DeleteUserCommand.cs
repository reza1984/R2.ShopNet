using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Command to soft delete a user.
/// </summary>
public record DeleteUserCommand(Guid UserId) : ICommand<Result<bool>>;
