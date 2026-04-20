using R2.ShopNet.Framework.Common;
using R2.ShopNet.Framework.CQRS;

namespace R2.ShopNet.Identity.Application.Commands;

/// <summary>
/// Command to initiate password reset process.
/// </summary>
public record ForgotPasswordCommand(string Email) : ICommand<Result<ForgotPasswordResponse>>;
