namespace R2.ShopNet.Identity.Application.Commands.RegisterPasskey;

/// <summary>
/// Response containing passkey registration options.
/// </summary>
public record RegisterPasskeyResponse
{
    /// <summary>
    /// WebAuthn registration options in JSON format.
    /// </summary>
    public required string RegistrationOptionsJson { get; init; }

    /// <summary>
    /// Base64-encoded challenge for the client to sign.
    /// </summary>
    public required string Challenge { get; init; }

    /// <summary>
    /// Success message or instructions for the user.
    /// </summary>
    public string Message { get; init; } = "Use your device's biometric authentication to register your passkey.";
}
