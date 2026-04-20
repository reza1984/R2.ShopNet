using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Interfaces;

namespace R2.ShopNet.Identity.Application.Commands.Passkey;

public class DeletePasskeyCommandHandler : ICommandHandler<DeletePasskeyCommand, Result<bool>>
{
    private readonly IPasskeyService _passkeyService;
    private readonly ILogger<DeletePasskeyCommandHandler> _logger;

    public DeletePasskeyCommandHandler(
        IPasskeyService passkeyService,
        ILogger<DeletePasskeyCommandHandler> logger)
    {
        _passkeyService = passkeyService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeletePasskeyCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _passkeyService.DeleteCredentialAsync(request.UserId, request.CredentialId, cancellationToken);

        if (!deleted)
        {
            return Result.Failure<bool>(
                Error.Failure("DeletePasskeyFailed", "Passkey credential not found or already deleted"));
        }

        return Result<bool>.Success(true);
    }
}
