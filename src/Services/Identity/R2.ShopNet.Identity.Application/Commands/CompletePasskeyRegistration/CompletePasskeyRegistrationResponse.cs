namespace R2.ShopNet.Identity.Application.Commands.CompletePasskeyRegistration;

/// <summary>
/// Response after successfully completing passkey registration.
/// </summary>
public record CompletePasskeyRegistrationResponse
{
    /// <summary>
    /// The ID of the registered passkey credential.
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// The friendly name assigned to the passkey.
    /// </summary>
    public string? FriendlyName { get; init; }

    /// <summary>
    /// Success message.
    /// </summary>
    public string Message { get; init; } = "Passkey registered successfully.";
}
