namespace R2.ShopNet.Identity.Application.Commands.RegisterPasskey;

/// <summary>
/// Response containing passkey registration options.
/// The client will use these options to create a new passkey.
/// </summary>
public record RegisterPasskeyResponse
{
    /// <summary>
    /// Base64-encoded registration options JSON.
    /// The client should decode and use this with the WebAuthn API.
    /// </summary>
    public required string RegistrationOptionsJson { get; init; }

    /// <summary>
    /// Challenge string that must be returned when completing registration.
    /// </summary>
    public required string Challenge { get; init; }

    /// <summary>
    /// Message to display to the user.
    /// </summary>
    public string Message { get; init; } = "Scan your fingerprint or use your device's biometric authentication.";
}
