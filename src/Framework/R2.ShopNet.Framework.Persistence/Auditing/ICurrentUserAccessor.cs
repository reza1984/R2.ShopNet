namespace R2.ShopNet.Framework.Persistence.Auditing;

/// <summary>
/// Interface for accessing the current user identifier for audit tracking.
/// Implement this in your application to provide user context.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Gets the current user's identifier (username, email, or user ID).
    /// Returns null if no user is authenticated.
    /// </summary>
    string? GetCurrentUserId();

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
