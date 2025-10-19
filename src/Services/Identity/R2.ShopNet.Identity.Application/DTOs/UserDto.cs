namespace R2.ShopNet.Identity.Application.DTOs;

/// <summary>
/// Data transfer object for user information.
/// </summary>
public record UserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string FullName,
    bool IsActive,
    bool EmailConfirmed,
    string? PhoneNumber,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<string> Roles);
