namespace R2.ShopNet.Identity.Application.Commands.RegisterUser;

/// <summary>
/// Response for successful user registration.
/// </summary>
public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName);
