using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.CompletePasskeyRegistration;

/// <summary>
/// Command to complete passkey registration after client attestation.
/// </summary>
public record CompletePasskeyRegistrationCommand : ICommand<Result<CompletePasskeyRegistrationResponse>>
{
    /// <summary>
    /// The user ID for whom the passkey is being registered.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// JSON string containing the WebAuthn attestation response.
    /// </summary>
    public required string AttestationResponseJson { get; init; }

    /// <summary>
    /// Optional friendly name for the passkey.
    /// </summary>
    public string? FriendlyName { get; init; }

    /// <summary>
    /// User agent string from the request.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    public string? IpAddress { get; init; }
}
