using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.LoginWithPasskey;

/// <summary>
/// Command to authenticate a user using a passkey.
/// </summary>
public record LoginWithPasskeyCommand : ICommand<Result<LoginWithPasskeyResponse>>
{
    /// <summary>
    /// JSON string containing the WebAuthn assertion response.
    /// </summary>
    public required string AssertionResponseJson { get; init; }

    /// <summary>
    /// Optional username for conditional UI or user identification.
    /// </summary>
    public string? Username { get; init; }
}
