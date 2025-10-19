using R2.ShopNet.Framework.Events;

namespace R2.ShopNet.Identity.Domain.Events;

/// <summary>
/// Event raised when a new user is registered.
/// </summary>
public record UserRegisteredEvent : BaseEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }

    public UserRegisteredEvent(Guid userId, string email, string? firstName, string? lastName)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}
