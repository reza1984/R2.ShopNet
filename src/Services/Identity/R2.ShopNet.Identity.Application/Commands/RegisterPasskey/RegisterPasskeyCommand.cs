using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.RegisterPasskey;

/// <summary>
/// Command to initiate passkey registration for a user.
/// </summary>
public record RegisterPasskeyCommand : ICommand<Result<RegisterPasskeyResponse>>
{
    /// <summary>
    /// The user ID for whom the passkey is being registered.
    /// </summary>
    public required Guid UserId { get; init; }

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
