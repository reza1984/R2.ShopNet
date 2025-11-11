using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.BeginPasskeyLogin;

/// <summary>
/// Command to begin passkey authentication.
/// Generates WebAuthn assertion options for the user.
/// </summary>
public record BeginPasskeyLoginCommand : ICommand<Result<BeginPasskeyLoginResponse>>
{
    /// <summary>
    /// User's email address to identify which passkeys to allow.
    /// </summary>
    public required string Email { get; init; }
}
