using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.Passkey;

public record DeletePasskeyCommand(Guid UserId, Guid CredentialId) : ICommand<Result<bool>>;
