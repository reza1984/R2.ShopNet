using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.DTOs;

namespace R2.ShopNet.Identity.Application.Queries.GetUserPasskeys;

/// <summary>
/// Query to get all passkeys for a specific user.
/// </summary>
public record GetUserPasskeysQuery(Guid UserId) : IQuery<Result<List<PasskeyDto>>>;
