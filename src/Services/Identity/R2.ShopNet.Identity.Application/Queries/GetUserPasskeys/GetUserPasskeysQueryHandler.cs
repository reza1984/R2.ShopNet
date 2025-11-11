using Microsoft.EntityFrameworkCore;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Framework.CQRS.Attributes;
using R2.ShopNet.Identity.Application.DTOs;
using R2.ShopNet.Identity.Infrastructure.Persistence;

namespace R2.ShopNet.Identity.Application.Queries.GetUserPasskeys;

/// <summary>
/// Handler for getting all passkeys for a user.
/// </summary>
[GenerateHandler]
public class GetUserPasskeysQueryHandler : IQueryHandler<GetUserPasskeysQuery, Result<List<PasskeyDto>>>
{
    private readonly IdentityDbContext _dbContext;

    public GetUserPasskeysQueryHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<PasskeyDto>>> Handle(
        GetUserPasskeysQuery query,
        CancellationToken cancellationToken)
    {
        // Query passkeys for the user from the database
        var passkeys = await _dbContext.UserPasskeys
            .Where(p => p.UserId == query.UserId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var passkeyDtos = passkeys.Select(p => new PasskeyDto
        {
            Id = p.Id.ToString(),
            UserId = p.UserId.ToString(),
            FriendlyName = p.Name ?? "Unnamed Passkey",
            CredentialId = Convert.ToBase64String(p.CredentialId),
            CreatedAt = p.CreatedAt,
            LastUsedAt = p.LastUsedAt,
            UserAgent = p.UserAgent,
            IpAddress = p.IpAddress,
            IsActive = p.IsActive
        }).ToList();

        return Result.Success(passkeyDtos);
    }
}
