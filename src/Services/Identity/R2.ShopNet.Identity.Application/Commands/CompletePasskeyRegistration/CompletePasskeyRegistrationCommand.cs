using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.CompletePasskeyRegistration;

/// <summary>
/// Command to complete passkey registration after the client has created the credential.
/// </summary>
public record CompletePasskeyRegistrationCommand : ICommand<Result<CompletePasskeyRegistrationResponse>>
{
    /// <summary>
    /// The ID of the user completing passkey registration.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The attestation response from the WebAuthn API (as JSON string).
    /// </summary>
    public required string AttestationResponseJson { get; init; }

    /// <summary>
    /// Optional friendly name for the passkey.
    /// </summary>
    public string? FriendlyName { get; init; }

    /// <summary>
    /// User agent string from the browser.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? IpAddress { get; init; }
}
