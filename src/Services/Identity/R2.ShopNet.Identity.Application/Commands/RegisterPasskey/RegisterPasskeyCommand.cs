using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands.RegisterPasskey;

/// <summary>
/// Command to initiate passkey registration for a user.
/// Returns registration options that the client will use to create a passkey.
/// </summary>
public record RegisterPasskeyCommand : ICommand<Result<RegisterPasskeyResponse>>
{
    /// <summary>
    /// The ID of the user registering a passkey.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Optional friendly name for the passkey (e.g., "MacBook Touch ID", "iPhone Face ID").
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
