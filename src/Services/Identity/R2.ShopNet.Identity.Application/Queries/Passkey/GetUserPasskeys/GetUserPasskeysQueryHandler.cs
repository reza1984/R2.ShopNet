using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.DTOs.Passkey;
using R2.ShopNet.Identity.Application.Interfaces;

namespace R2.ShopNet.Identity.Application.Queries;

public class GetUserPasskeysQueryHandler : IQueryHandler<GetUserPasskeysQuery, Result<IEnumerable<PasskeyCredentialDto>>>
{
    private readonly IPasskeyService _passkeyService;

    public GetUserPasskeysQueryHandler(IPasskeyService passkeyService)
    {
        _passkeyService = passkeyService;
    }

    public async Task<Result<IEnumerable<PasskeyCredentialDto>>> Handle(GetUserPasskeysQuery request, CancellationToken cancellationToken)
    {
        var credentials = await _passkeyService.GetUserCredentialsAsync(request.UserId, cancellationToken);
        return Result.Success(credentials);
    }
}
