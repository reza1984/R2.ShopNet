using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.DTOs.Passkey;

namespace R2.ShopNet.Identity.Application.Queries.Passkey.GetUserPasskeys;

public record GetUserPasskeysQuery(Guid UserId) : IQuery<Result<IEnumerable<PasskeyCredentialDto>>>;
