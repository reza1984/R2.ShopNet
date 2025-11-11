using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Domain.Entities;

namespace R2.ShopNet.Identity.Application.Queries.GetUserPasskeys;

/// <summary>
/// Handler for retrieving a user's passkeys.
/// </summary>
public class GetUserPasskeysQueryHandler : IQueryHandler<GetUserPasskeysQuery, List<PasskeyDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<GetUserPasskeysQueryHandler> _logger;

    public GetUserPasskeysQueryHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<GetUserPasskeysQueryHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<List<PasskeyDto>> Handle(GetUserPasskeysQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving passkeys for user: {UserId}", query.UserId);

        // Get user
        var user = await _userManager.FindByIdAsync(query.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", query.UserId);
            return new List<PasskeyDto>();
        }

        // Get user's passkeys
        var passkeys = await _userManager.GetPasskeysAsync(user);

        _logger.LogInformation("Found {Count} passkeys for user: {UserId}", passkeys.Count, query.UserId);

        // Map to DTOs using the data available from .NET Identity API
        var passkeyDtos = passkeys.Select(p => new PasskeyDto
        {
            Id = Convert.ToBase64String(p.CredentialId),
            UserId = query.UserId.ToString(),
            FriendlyName = p.Name ?? "Unnamed Passkey",
            CredentialId = Convert.ToBase64String(p.CredentialId),
            CreatedAt = DateTime.UtcNow, // UserPasskeyInfo doesn't expose CreatedAt
            LastUsedAt = null, // Not tracked by ASP.NET Core Identity
            IsActive = true,
            UserAgent = null, // Not exposed by UserPasskeyInfo API
            IpAddress = null // Not exposed by UserPasskeyInfo API
        }).ToList();

        return passkeyDtos;
    }
}
