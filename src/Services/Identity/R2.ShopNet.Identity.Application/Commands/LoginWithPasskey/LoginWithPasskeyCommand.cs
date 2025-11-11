using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.LoginWithPasskey;

/// <summary>
/// Command to authenticate a user using a passkey.
/// </summary>
public record LoginWithPasskeyCommand : ICommand<Result<LoginWithPasskeyResponse>>
{
    /// <summary>
    /// The assertion response from the WebAuthn API (as JSON string).
    /// </summary>
    public required string AssertionResponseJson { get; init; }

    /// <summary>
    /// Optional username/email to improve UX (filters passkeys for that user).
    /// </summary>
    public string? Username { get; init; }
}
