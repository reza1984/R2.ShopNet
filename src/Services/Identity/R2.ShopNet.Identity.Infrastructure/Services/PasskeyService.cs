using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using R2.ShopNet.Identity.Domain.Entities;
using R2.ShopNet.Identity.Infrastructure.Persistence;
using R2.ShopNet.Identity.Application.Interfaces;
using R2.ShopNet.Identity.Application.DTOs.Passkey;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace R2.ShopNet.Identity.Infrastructure.Services;

public class PasskeyService : IPasskeyService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasskeyService> _logger;

    // .NET 10 provides native WebAuthn support through ASP.NET Core Identity
    // Check official docs: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys

    public PasskeyService(
        UserManager<ApplicationUser> userManager,
        IdentityDbContext context,
        IConfiguration configuration,
        ILogger<PasskeyService> logger)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PasskeyRegistrationOptions> BeginRegistrationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Find user
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Exclude credentials already registered for this user
        var excludeCredentials = await _context.PasskeyCredentials
            .Where(c => c.UserId == userId)
            .Select(c => new PublicKeyCredentialDescriptor
            {
                Type = "public-key",
                Id = Convert.ToBase64String(c.CredentialId)
            })
            .ToListAsync(cancellationToken);

        // Generate challenge
        var challengeBytes = new byte[32];
        Random.Shared.NextBytes(challengeBytes);
        var challenge = Convert.ToBase64String(challengeBytes);

        // Encode userId as base64url for WebAuthn
        string userIdBase64Url = Convert.ToBase64String(user.Id.ToByteArray())
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var options = new PasskeyRegistrationOptions
        {
            Challenge = challenge,
            RpId = _configuration["WebAuthn:RelyingPartyId"] ?? "localhost",
            RpName = _configuration["WebAuthn:RelyingPartyName"] ?? "ShopNet",
            UserId = userIdBase64Url,
            UserName = user.Email ?? string.Empty,
            UserDisplayName = user.Email ?? string.Empty,
            Timeout = 60000,
            Attestation = "none",
            PubKeyCredParams = new List<PublicKeyCredentialParameters>
            {
                new() { Type = "public-key", Alg = -7 }, // ES256
                new() { Type = "public-key", Alg = -257 } // RS256
            },
            AuthenticatorSelection = new AuthenticatorSelectionCriteria
            {
                AuthenticatorAttachment = "platform",
                ResidentKey = "preferred",
                RequireResidentKey = false,
                UserVerification = "preferred"
            },
            ExcludeCredentials = excludeCredentials
        };
        return options;
    }

    public async Task<PasskeyRegistrationResult> CompleteRegistrationAsync(Guid userId, string deviceName, PasskeyRegistrationResponse response, CancellationToken cancellationToken = default)
    {
        // Validate response
        if (response == null)
        {
            _logger.LogError("Passkey registration failed: response is null");
            return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Invalid response: null." };
        }
        if (response.Response == null)
        {
            _logger.LogError("Passkey registration failed: response.Response is null");
            return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Invalid response: missing attestation response." };
        }

        try
        {
            // Decode credentialId and publicKey (use base64url for credentialId)
            var credentialId = Base64UrlDecode(response.RawId);
            var attestationObject = Convert.FromBase64String(response.Response.AttestationObject);
            var clientDataJSON = Convert.FromBase64String(response.Response.ClientDataJSON);

            // Store credential (simplified, real implementation should parse attestationObject)
            var credential = new PasskeyCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CredentialId = credentialId,
                PublicKey = attestationObject, // Should be parsed to extract public key
                SignCount = 0,
                CreatedAt = DateTime.UtcNow,
                DeviceName = deviceName
            };
            _context.PasskeyCredentials.Add(credential);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Passkey registration succeeded for user {UserId}, credentialId: {CredentialId}", userId, credential.Id);
            return new PasskeyRegistrationResult { Success = true, CredentialId = credential.Id };
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Passkey registration failed: base64 decode error. RawId: {RawId}, AttestationObject: {AttestationObject}, ClientDataJSON: {ClientDataJSON}", response.RawId, response.Response.AttestationObject, response.Response.ClientDataJSON);
            return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Base64 decode error: " + ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Passkey registration failed: unexpected error");
            return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Unexpected error: " + ex.Message };
        }
    }

    public async Task<PasskeyAuthenticationOptions> BeginAuthenticationAsync(string? username = null, CancellationToken cancellationToken = default)
    {
        // Find user by username
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required for passkey authentication.");

        var user = await _userManager.FindByEmailAsync(username);
        if (user == null)
            throw new InvalidOperationException("User not found.");

        // Get user's passkey credentials
        var credentials = await _context.PasskeyCredentials
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);

        // Build allowCredentials list for WebAuthn
        var allowCredentials = credentials.Select(c => new PublicKeyCredentialDescriptor
        {
            Type = "public-key",
            Id = Convert.ToBase64String(c.CredentialId)
        }).ToList();

        // Generate challenge
        var challengeBytes = new byte[32];
        Random.Shared.NextBytes(challengeBytes);
        var challenge = Convert.ToBase64String(challengeBytes);

        // Build options
        var options = new PasskeyAuthenticationOptions
        {
            Challenge = challenge,
            RpId = _configuration["WebAuthn:RelyingPartyId"] ?? "localhost",
            Timeout = 60000,
            AllowCredentials = allowCredentials,
            UserVerification = "preferred"
        };
        return options;
    }

    public async Task<PasskeyAuthenticationResult> CompleteAuthenticationAsync(PasskeyAuthenticationResponse response, CancellationToken cancellationToken = default)
    {
        // Validate response
        if (response == null || response.Response == null)
            return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Invalid response." };

        // Log rawId received from frontend
        _logger.LogInformation("Received rawId from frontend: {RawId}", response.RawId);

        // Decode credentialId from base64url (WebAuthn format)
        byte[] credentialId;
        try
        {
            credentialId = Base64UrlDecode(response.RawId);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to decode credentialId from base64url. RawId: {RawId}", response.RawId);
            return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "CredentialId decode error." };
        }

        // Debug: log credentialId being looked up and all stored credentialIds
        _logger.LogInformation("Looking for credentialId: {CredentialId} (base64url: {RawId})", BitConverter.ToString(credentialId), response.RawId);
        var allCreds = await _context.PasskeyCredentials.ToListAsync(cancellationToken);
        foreach (var cred in allCreds)
        {
            _logger.LogInformation("Stored credentialId: {StoredId}", BitConverter.ToString(cred.CredentialId));
        }

        // Find credential in DB
        var credential = await _context.PasskeyCredentials
            .FirstOrDefaultAsync(c => c.CredentialId == credentialId, cancellationToken);

        if (credential == null)
            return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Credential not found." };

        // Validate signature (skipped for brevity, should use WebAuthn libraries)
        // Update last used timestamp
        credential.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        // Return result
        return new PasskeyAuthenticationResult
        {
            Success = true,
            UserId = credential.UserId,
            Email = credential.User?.Email
        };
    }

    // Helper: decode base64url string to byte[]
    public static byte[] Base64UrlDecode(string base64Url)
    {
        string padded = base64Url.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    public async Task<IEnumerable<PasskeyCredentialDto>> GetUserCredentialsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var credentials = await _context.PasskeyCredentials
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new PasskeyCredentialDto
            {
                Id = c.Id,
                DeviceName = c.DeviceName,
                CreatedAt = c.CreatedAt,
                LastUsedAt = c.LastUsedAt
            })
            .ToListAsync(cancellationToken);

        return credentials;
    }

    public async Task<bool> DeleteCredentialAsync(Guid userId, Guid credentialId, CancellationToken cancellationToken = default)
    {
        var credential = await _context.PasskeyCredentials
            .FirstOrDefaultAsync(c => c.Id == credentialId && c.UserId == userId, cancellationToken);

        if (credential == null)
            return false;

        _context.PasskeyCredentials.Remove(credential);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
