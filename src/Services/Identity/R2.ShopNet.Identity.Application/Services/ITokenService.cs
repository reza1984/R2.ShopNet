using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Services;

/// <summary>
/// Service interface for JWT token generation using ASP.NET Core Identity.
/// </summary>
public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(ApplicationUser user);
    Task<string> GenerateIdTokenAsync(ApplicationUser user);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
}
