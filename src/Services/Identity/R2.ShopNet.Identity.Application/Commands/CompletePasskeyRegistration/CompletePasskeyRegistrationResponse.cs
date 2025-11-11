namespace R2.ShopNet.Identity.Application.Commands.CompletePasskeyRegistration;

/// <summary>
/// Response after successful passkey registration completion.
/// </summary>
public record CompletePasskeyRegistrationResponse
{
    /// <summary>
    /// Base64-encoded credential ID of the registered passkey.
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// Friendly name for the registered passkey.
    /// </summary>
    public string? FriendlyName { get; init; }

    /// <summary>
    /// Success message.
    /// </summary>
    public string Message { get; init; } = "Passkey registered successfully.";
}
