using R2.ShopNet.Identity.Application.DTOs.Passkey;

namespace R2.ShopNet.Identity.Application.Interfaces;

public interface IPasskeyService
{
    // Registration flow
    Task<PasskeyRegistrationOptions> BeginRegistrationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PasskeyRegistrationResult> CompleteRegistrationAsync(Guid userId, string deviceName, PasskeyRegistrationResponse response, CancellationToken cancellationToken = default);

    // Authentication flow
    Task<PasskeyAuthenticationOptions> BeginAuthenticationAsync(string? username = null, CancellationToken cancellationToken = default);
    Task<PasskeyAuthenticationResult> CompleteAuthenticationAsync(PasskeyAuthenticationResponse response, CancellationToken cancellationToken = default);

    // Credential management
    Task<IEnumerable<PasskeyCredentialDto>> GetUserCredentialsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteCredentialAsync(Guid userId, Guid credentialId, CancellationToken cancellationToken = default);
}
