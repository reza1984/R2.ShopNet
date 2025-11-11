namespace R2.ShopNet.Identity.Application.Commands.BeginPasskeyLogin;

/// <summary>
/// Response containing WebAuthn assertion options for passkey authentication.
/// </summary>
public record BeginPasskeyLoginResponse
{
    /// <summary>
    /// JSON string containing WebAuthn assertion options.
    /// This should be parsed by the client and passed to navigator.credentials.get().
    /// </summary>
    public required string AssertionOptionsJson { get; init; }

    /// <summary>
    /// Base64url-encoded challenge for debugging/logging purposes.
    /// </summary>
    public required string Challenge { get; init; }
}
