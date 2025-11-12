using Microsoft.Extensions.Logging;
using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;
using R2.ShopNet.Identity.Application.Interfaces;

namespace R2.ShopNet.Identity.Application.Commands.Passkey.RegisterPasskey;

public class RegisterPasskeyCommandHandler : ICommandHandler<RegisterPasskeyCommand, Result<Guid>>
{
    private readonly IPasskeyService _passkeyService;
    private readonly ILogger<RegisterPasskeyCommandHandler> _logger;

    public RegisterPasskeyCommandHandler(
        IPasskeyService passkeyService,
        ILogger<RegisterPasskeyCommandHandler> logger)
    {
        _passkeyService = passkeyService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RegisterPasskeyCommand request, CancellationToken cancellationToken)
    {
        var result = await _passkeyService.CompleteRegistrationAsync(
            request.UserId,
            request.DeviceName ?? "Unnamed Device",
            request.Response,
            cancellationToken);

        if (!result.Success)
        {
            return Result.Failure<Guid>(Error.Failure("PasskeyRegistrationFailed", result.ErrorMessage ?? "Passkey registration failed"));
        }

        return Result<Guid>.Success(result.CredentialId!.Value);
    }
}
