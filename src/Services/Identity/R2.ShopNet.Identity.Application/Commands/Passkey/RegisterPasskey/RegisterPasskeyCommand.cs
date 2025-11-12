using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.DTOs.Passkey;

namespace R2.ShopNet.Identity.Application.Commands.Passkey.RegisterPasskey;

public record RegisterPasskeyCommand(
    Guid UserId,
    PasskeyRegistrationResponse Response,
    string? DeviceName = null
) : ICommand<Result<Guid>>;
