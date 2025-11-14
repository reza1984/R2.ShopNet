using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using R2.ShopNet.Identity.Domain.Entities;
using R2.ShopNet.Identity.Infrastructure.Persistence;
using R2.ShopNet.Identity.Application.Interfaces;
using R2.ShopNet.Identity.Application.DTOs.Passkey;
using Microsoft.EntityFrameworkCore;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace R2.ShopNet.Identity.Infrastructure.Services;

/// <summary>
/// Passkey service implementing WebAuthn Level 2 specification
/// https://www.w3.org/TR/webauthn-2/
/// </summary>
public class PasskeyService : IPasskeyService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasskeyService> _logger;
    private readonly IDistributedCache? _cache;

    private const int ChallengeExpirationMinutes = 5;

    public PasskeyService(
        UserManager<ApplicationUser> userManager,
        IdentityDbContext context,
        IConfiguration configuration,
        ILogger<PasskeyService> logger,
        IDistributedCache? cache = null)
    {
        _userManager = userManager;
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
    }

    public async Task<PasskeyRegistrationOptions> BeginRegistrationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("BeginRegistration failed: User {UserId} not found", userId);
            throw new InvalidOperationException("User not found.");
        }

        // Exclude credentials already registered for this user
        var excludeCredentials = await _context.PasskeyCredentials
            .Where(c => c.UserId == userId)
            .Select(c => new PublicKeyCredentialDescriptor
            {
                Type = "public-key",
                Id = Base64UrlEncode(c.CredentialId)
            })
            .ToListAsync(cancellationToken);

        // Generate cryptographically secure challenge
        var challengeBytes = RandomNumberGenerator.GetBytes(32);
        var challenge = Base64UrlEncode(challengeBytes);

        // Store challenge with expiration for later validation
        await StoreChallengeAsync($"reg:{userId}", challengeBytes, cancellationToken);

        // Encode userId as base64url for WebAuthn
        var userIdBase64Url = Base64UrlEncode(userId.ToByteArray());

        var rpId = _configuration["WebAuthn:RelyingPartyId"] ?? "localhost";
        var rpName = _configuration["WebAuthn:RelyingPartyName"] ?? "R2.ShopNet";

        var options = new PasskeyRegistrationOptions
        {
            Challenge = challenge,
            RpId = rpId,
            RpName = rpName,
            UserId = userIdBase64Url,
            UserName = user.Email ?? user.UserName ?? string.Empty,
            UserDisplayName = $"{user.FirstName} {user.LastName}".Trim() ?? user.Email ?? string.Empty,
            Timeout = 60000, // 1 minute
            Attestation = "none", // For privacy, don't require attestation
            PubKeyCredParams = new List<PublicKeyCredentialParameters>
            {
                new() { Type = "public-key", Alg = -7 },   // ES256 (recommended)
                new() { Type = "public-key", Alg = -257 }, // RS256 (widely supported)
                // new() { Type = "public-key", Alg = -8 }    // EdDSA (modern)
            },
            AuthenticatorSelection = new AuthenticatorSelectionCriteria
            {
                AuthenticatorAttachment = "platform", // Prefer platform authenticators (Touch ID, Face ID, Windows Hello)
                ResidentKey = "preferred",
                RequireResidentKey = false,
                UserVerification = "preferred" // Request user verification but don't require it
            },
            ExcludeCredentials = excludeCredentials
        };

        _logger.LogInformation("BeginRegistration succeeded for user {UserId}", userId);
        return options;
    }

    public async Task<PasskeyRegistrationResult> CompleteRegistrationAsync(Guid userId, string deviceName, PasskeyRegistrationResponse response, CancellationToken cancellationToken = default)
    {
        if (response?.Response == null)
        {
            _logger.LogError("CompleteRegistration failed: Invalid response structure");
            return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Invalid response structure." };
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("CompleteRegistration failed: User {UserId} not found", userId);
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "User not found." };
            }

            // Decode base64 data
            var credentialId = Base64UrlDecode(response.RawId);
            var attestationObject = Base64Decode(response.Response.AttestationObject);
            var clientDataJSON = Base64Decode(response.Response.ClientDataJSON);

            // Log raw client data for debugging
            var clientDataString = Encoding.UTF8.GetString(clientDataJSON);
            _logger.LogInformation("CompleteRegistration: Client data JSON: {ClientDataJSON}", clientDataString);

            // Validate client data
            var clientData = JsonSerializer.Deserialize<ClientData>(clientDataJSON);
            if (clientData == null)
            {
                _logger.LogWarning("CompleteRegistration failed: Client data is null");
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Invalid client data structure." };
            }

            _logger.LogInformation("CompleteRegistration: Parsed client data - Type: {Type}, Origin: {Origin}, Challenge: {Challenge}",
                clientData.Type, clientData.Origin, clientData.Challenge);

            if (clientData.Type != "webauthn.create")
            {
                _logger.LogWarning("CompleteRegistration failed: Invalid client data type. Expected 'webauthn.create', got '{Type}'", clientData.Type);
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Invalid client data type." };
            }

            // Verify challenge
            var storedChallenge = await GetChallengeAsync($"reg:{userId}", cancellationToken);
            if (storedChallenge == null)
            {
                _logger.LogWarning("CompleteRegistration failed: Challenge expired or not found");
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Challenge expired or not found." };
            }

            var receivedChallenge = Base64UrlDecode(clientData.Challenge);
            if (!storedChallenge.SequenceEqual(receivedChallenge))
            {
                _logger.LogWarning("CompleteRegistration failed: Challenge mismatch");
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Challenge verification failed." };
            }

            // Verify origin
            var expectedOrigin = _configuration["WebAuthn:Origin"] ?? "https://localhost";
            if (!clientData.Origin.Equals(expectedOrigin, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CompleteRegistration failed: Origin mismatch. Expected: {Expected}, Got: {Actual}",
                    expectedOrigin, clientData.Origin);
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Origin verification failed." };
            }

            // Parse attestation object (CBOR)
            var attestationData = ParseAttestationObject(attestationObject);
            if (attestationData == null)
            {
                _logger.LogError("CompleteRegistration failed: Failed to parse attestation object");
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Failed to parse attestation object." };
            }

            // Verify RP ID hash
            var rpId = _configuration["WebAuthn:RelyingPartyId"] ?? "localhost";
            var rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
            if (!rpIdHash.SequenceEqual(attestationData.RpIdHash))
            {
                _logger.LogWarning("CompleteRegistration failed: RP ID hash mismatch");
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "RP ID verification failed." };
            }

            // Verify user present flag
            if (!attestationData.UserPresent)
            {
                _logger.LogWarning("CompleteRegistration failed: User presence flag not set");
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "User presence verification failed." };
            }

            // Check for duplicate credential
            var existingCredential = await _context.PasskeyCredentials
                .FirstOrDefaultAsync(c => c.CredentialId == credentialId, cancellationToken);
            if (existingCredential != null)
            {
                _logger.LogWarning("CompleteRegistration failed: Credential already exists");
                return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Credential already registered." };
            }

            // Store credential with extracted public key
            var credential = new PasskeyCredential
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CredentialId = credentialId,
                PublicKey = attestationData.PublicKey, // Store the actual public key, not attestation object
                SignCount = attestationData.SignCount,
                CreatedAt = DateTime.UtcNow,
                DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "Passkey" : deviceName
            };

            _context.PasskeyCredentials.Add(credential);
            await _context.SaveChangesAsync(cancellationToken);

            // Clean up challenge
            await DeleteChallengeAsync($"reg:{userId}", cancellationToken);

            _logger.LogInformation("CompleteRegistration succeeded for user {UserId}, credential {CredentialId}",
                userId, credential.Id);
            return new PasskeyRegistrationResult { Success = true, CredentialId = credential.Id };
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "CompleteRegistration failed: Base64 decode error");
            return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Invalid encoding format." };
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "CompleteRegistration failed: Cryptographic error");
            return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Cryptographic verification failed." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompleteRegistration failed: Unexpected error");
            return new PasskeyRegistrationResult { Success = false, ErrorMessage = "Registration failed." };
        }
    }

    public async Task<PasskeyAuthenticationOptions> BeginAuthenticationAsync(string? username = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("BeginAuthentication failed: Username is required");
            throw new ArgumentException("Username is required for passkey authentication.", nameof(username));
        }

        var user = await _userManager.FindByEmailAsync(username)
                    ?? await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            _logger.LogWarning("BeginAuthentication failed: User {Username} not found", username);
            throw new InvalidOperationException("User not found.");
        }

        // Get user's passkey credentials
        var credentials = await _context.PasskeyCredentials
            .Where(c => c.UserId == user.Id)
            .ToListAsync(cancellationToken);

        if (!credentials.Any())
        {
            _logger.LogWarning("BeginAuthentication failed: No passkeys registered for user {UserId}", user.Id);
            throw new InvalidOperationException("No passkeys registered for this account.");
        }

        // Build allowCredentials list for WebAuthn
        var allowCredentials = credentials.Select(c => new PublicKeyCredentialDescriptor
        {
            Type = "public-key",
            Id = Base64UrlEncode(c.CredentialId)
        }).ToList();

        // Generate cryptographically secure challenge
        var challengeBytes = RandomNumberGenerator.GetBytes(32);
        var challenge = Base64UrlEncode(challengeBytes);

        // Store challenge with expiration for later validation
        await StoreChallengeAsync($"auth:{user.Id}", challengeBytes, cancellationToken);

        var rpId = _configuration["WebAuthn:RelyingPartyId"] ?? "localhost";

        var options = new PasskeyAuthenticationOptions
        {
            Challenge = challenge,
            RpId = rpId,
            Timeout = 60000, // 1 minute
            AllowCredentials = allowCredentials,
            UserVerification = "preferred"
        };

        _logger.LogInformation("BeginAuthentication succeeded for user {UserId}", user.Id);
        return options;
    }

    public async Task<PasskeyAuthenticationResult> CompleteAuthenticationAsync(PasskeyAuthenticationResponse response, CancellationToken cancellationToken = default)
    {
        if (response?.Response == null)
        {
            _logger.LogError("CompleteAuthentication failed: Invalid response structure");
            return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Invalid response structure." };
        }

        try
        {
            // Decode base64 data
            var credentialId = Base64UrlDecode(response.RawId);
            var clientDataJSON = Base64Decode(response.Response.ClientDataJSON);
            var authenticatorData = Base64Decode(response.Response.AuthenticatorData);
            var signature = Base64Decode(response.Response.Signature);
            var userHandle = string.IsNullOrEmpty(response.Response.UserHandle)
                ? null
                : Base64UrlDecode(response.Response.UserHandle);

            // Find credential in database
            var credential = await _context.PasskeyCredentials
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CredentialId.SequenceEqual(credentialId), cancellationToken);

            if (credential == null)
            {
                _logger.LogWarning("CompleteAuthentication failed: Credential not found");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Credential not found." };
            }

            // Validate client data
            var clientData = JsonSerializer.Deserialize<ClientData>(clientDataJSON);
            if (clientData == null || clientData.Type != "webauthn.get")
            {
                _logger.LogWarning("CompleteAuthentication failed: Invalid client data type");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Invalid client data type." };
            }

            // Verify challenge
            var storedChallenge = await GetChallengeAsync($"auth:{credential.UserId}", cancellationToken);
            if (storedChallenge == null)
            {
                _logger.LogWarning("CompleteAuthentication failed: Challenge expired or not found");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Challenge expired or not found." };
            }

            var receivedChallenge = Base64UrlDecode(clientData.Challenge);
            if (!storedChallenge.SequenceEqual(receivedChallenge))
            {
                _logger.LogWarning("CompleteAuthentication failed: Challenge mismatch");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Challenge verification failed." };
            }

            // Verify origin
            var expectedOrigin = _configuration["WebAuthn:Origin"] ?? "https://localhost";
            if (!clientData.Origin.Equals(expectedOrigin, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CompleteAuthentication failed: Origin mismatch. Expected: {Expected}, Got: {Actual}",
                    expectedOrigin, clientData.Origin);
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Origin verification failed." };
            }

            // Parse authenticator data
            var authData = ParseAuthenticatorData(authenticatorData);
            if (authData == null)
            {
                _logger.LogError("CompleteAuthentication failed: Failed to parse authenticator data");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Failed to parse authenticator data." };
            }

            // Verify RP ID hash
            var rpId = _configuration["WebAuthn:RelyingPartyId"] ?? "localhost";
            var rpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(rpId));
            if (!rpIdHash.SequenceEqual(authData.RpIdHash))
            {
                _logger.LogWarning("CompleteAuthentication failed: RP ID hash mismatch");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "RP ID verification failed." };
            }

            // Verify user present flag
            if (!authData.UserPresent)
            {
                _logger.LogWarning("CompleteAuthentication failed: User presence flag not set");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "User presence verification failed." };
            }

            // Verify signature count (prevent replay attacks)
            if (authData.SignCount > 0 && authData.SignCount <= credential.SignCount)
            {
                _logger.LogWarning("CompleteAuthentication failed: Sign count did not increase. Possible cloned authenticator");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Authentication failed due to suspicious activity." };
            }

            // Verify signature
            var isSignatureValid = VerifySignature(
                credential.PublicKey,
                authenticatorData,
                clientDataJSON,
                signature);

            if (!isSignatureValid)
            {
                _logger.LogWarning("CompleteAuthentication failed: Signature verification failed");
                return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Signature verification failed." };
            }

            // Update credential
            credential.SignCount = authData.SignCount;
            credential.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // Clean up challenge
            await DeleteChallengeAsync($"auth:{credential.UserId}", cancellationToken);

            _logger.LogInformation("CompleteAuthentication succeeded for user {UserId}", credential.UserId);
            return new PasskeyAuthenticationResult
            {
                Success = true,
                UserId = credential.UserId,
                Email = credential.User?.Email
            };
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "CompleteAuthentication failed: Base64 decode error");
            return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Invalid encoding format." };
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "CompleteAuthentication failed: Cryptographic error");
            return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Cryptographic verification failed." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CompleteAuthentication failed: Unexpected error");
            return new PasskeyAuthenticationResult { Success = false, ErrorMessage = "Authentication failed." };
        }
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

        _logger.LogInformation("Deleted credential {CredentialId} for user {UserId}", credentialId, userId);
        return true;
    }

    #region Helper Methods

    /// <summary>
    /// Store challenge with expiration
    /// </summary>
    private async Task StoreChallengeAsync(string key, byte[] challenge, CancellationToken cancellationToken)
    {
        if (_cache != null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ChallengeExpirationMinutes)
            };
            await _cache.SetAsync(key, challenge, options, cancellationToken);
        }
        // If no cache is configured, challenges won't be validated (less secure but still functional)
    }

    /// <summary>
    /// Retrieve stored challenge
    /// </summary>
    private async Task<byte[]?> GetChallengeAsync(string key, CancellationToken cancellationToken)
    {
        if (_cache != null)
        {
            return await _cache.GetAsync(key, cancellationToken);
        }
        return null; // If no cache, skip challenge validation
    }

    /// <summary>
    /// Delete challenge after successful authentication
    /// </summary>
    private async Task DeleteChallengeAsync(string key, CancellationToken cancellationToken)
    {
        if (_cache != null)
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
    }

    /// <summary>
    /// Base64Url encode (RFC 4648 Section 5)
    /// </summary>
    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Base64Url decode (RFC 4648 Section 5)
    /// </summary>
    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Standard Base64 decode (for clientDataJSON and attestationObject)
    /// </summary>
    private static byte[] Base64Decode(string base64)
    {
        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Parse CBOR-encoded attestation object
    /// https://www.w3.org/TR/webauthn-2/#sctn-attestation
    /// </summary>
    private AuthenticatorAttestationData? ParseAttestationObject(byte[] attestationObject)
    {
        try
        {
            var reader = new CborReader(attestationObject);
            reader.ReadStartMap();

            byte[]? authData = null;
            string? fmt = null;

            while (reader.PeekState() != CborReaderState.EndMap)
            {
                var key = reader.ReadTextString();
                switch (key)
                {
                    case "authData":
                        authData = reader.ReadByteString();
                        break;
                    case "fmt":
                        fmt = reader.ReadTextString();
                        break;
                    case "attStmt":
                        reader.ReadEncodedValue(); // Skip attestation statement
                        break;
                    default:
                        reader.ReadEncodedValue(); // Skip unknown fields
                        break;
                }
            }

            reader.ReadEndMap();

            if (authData == null)
            {
                _logger.LogError("ParseAttestationObject: authData is null");
                return null;
            }

            return ParseAuthenticatorData(authData, extractCredentialData: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse attestation object");
            return null;
        }
    }

    /// <summary>
    /// Parse authenticator data
    /// https://www.w3.org/TR/webauthn-2/#sctn-authenticator-data
    /// </summary>
    private AuthenticatorAttestationData? ParseAuthenticatorData(byte[] authData, bool extractCredentialData = false)
    {
        try
        {
            if (authData.Length < 37) // Minimum length: rpIdHash(32) + flags(1) + signCount(4)
            {
                _logger.LogError("ParseAuthenticatorData: authData too short");
                return null;
            }

            var result = new AuthenticatorAttestationData();
            var offset = 0;

            // RP ID hash (32 bytes)
            result.RpIdHash = authData[offset..(offset + 32)];
            offset += 32;

            // Flags (1 byte)
            var flags = authData[offset];
            offset += 1;

            result.UserPresent = (flags & 0x01) != 0;
            result.UserVerified = (flags & 0x04) != 0;
            var hasAttestedCredentialData = (flags & 0x40) != 0;
            var hasExtensions = (flags & 0x80) != 0;

            // Sign count (4 bytes, big-endian)
            result.SignCount = (uint)((authData[offset] << 24) | (authData[offset + 1] << 16) |
                                     (authData[offset + 2] << 8) | authData[offset + 3]);
            offset += 4;

            // Attested credential data (only present during registration)
            if (extractCredentialData && hasAttestedCredentialData)
            {
                if (authData.Length < offset + 18) // AAGUID(16) + credIdLen(2)
                {
                    _logger.LogError("ParseAuthenticatorData: Not enough data for credential");
                    return null;
                }

                // AAGUID (16 bytes) - skip
                offset += 16;

                // Credential ID length (2 bytes, big-endian)
                var credIdLen = (ushort)((authData[offset] << 8) | authData[offset + 1]);
                offset += 2;

                if (authData.Length < offset + credIdLen)
                {
                    _logger.LogError("ParseAuthenticatorData: Not enough data for credential ID");
                    return null;
                }

                // Credential ID - skip (we already have it from response.rawId)
                offset += credIdLen;

                // Public key (CBOR-encoded)
                if (authData.Length <= offset)
                {
                    _logger.LogError("ParseAuthenticatorData: No public key data");
                    return null;
                }

                result.PublicKey = authData[offset..];
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse authenticator data");
            return null;
        }
    }

    /// <summary>
    /// Verify signature using the stored public key
    /// </summary>
    private bool VerifySignature(byte[] publicKeyBytes, byte[] authenticatorData, byte[] clientDataJSON, byte[] signature)
    {
        try
        {
            // Parse COSE public key (CBOR-encoded)
            var coseKey = ParseCoseKey(publicKeyBytes);
            if (coseKey == null)
            {
                _logger.LogError("VerifySignature: Failed to parse COSE key");
                return false;
            }

            // Compute client data hash
            var clientDataHash = SHA256.HashData(clientDataJSON);

            // Concatenate authenticator data and client data hash
            var signedData = new byte[authenticatorData.Length + clientDataHash.Length];
            Buffer.BlockCopy(authenticatorData, 0, signedData, 0, authenticatorData.Length);
            Buffer.BlockCopy(clientDataHash, 0, signedData, authenticatorData.Length, clientDataHash.Length);

            // Verify signature based on algorithm
            return coseKey.Algorithm switch
            {
                -7 => VerifyES256Signature(coseKey, signedData, signature),   // ES256
                -257 => VerifyRS256Signature(coseKey, signedData, signature), // RS256
                // -8 => VerifyEdDSASignature(coseKey, signedData, signature),   // EdDSA
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Signature verification failed");
            return false;
        }
    }

    /// <summary>
    /// Parse COSE key from CBOR
    /// https://datatracker.ietf.org/doc/html/rfc8152#section-7
    /// </summary>
    private CoseKey? ParseCoseKey(byte[] coseKeyBytes)
    {
        try
        {
            var reader = new CborReader(coseKeyBytes);
            reader.ReadStartMap();

            var coseKey = new CoseKey();

            while (reader.PeekState() != CborReaderState.EndMap)
            {
                var label = reader.ReadInt32();
                switch (label)
                {
                    case 1: // kty (key type)
                        coseKey.KeyType = reader.ReadInt32();
                        break;
                    case 3: // alg (algorithm)
                        coseKey.Algorithm = reader.ReadInt32();
                        break;
                    case -1: // crv (curve) for EC keys
                        coseKey.Curve = reader.ReadInt32();
                        break;
                    case -2: // x-coordinate for EC keys, n for RSA
                        coseKey.X = reader.ReadByteString();
                        break;
                    case -3: // y-coordinate for EC keys, e for RSA
                        coseKey.Y = reader.ReadByteString();
                        break;
                    default:
                        reader.ReadEncodedValue(); // Skip unknown labels
                        break;
                }
            }

            reader.ReadEndMap();
            return coseKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse COSE key");
            return null;
        }
    }

    /// <summary>
    /// Verify ES256 (ECDSA P-256 with SHA-256) signature
    /// </summary>
    private bool VerifyES256Signature(CoseKey coseKey, byte[] signedData, byte[] signature)
    {
        try
        {
            if (coseKey.X == null || coseKey.Y == null)
                return false;

            using var ecdsa = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = coseKey.X,
                    Y = coseKey.Y
                }
            });

            return ecdsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ES256 signature verification failed");
            return false;
        }
    }

    /// <summary>
    /// Verify RS256 (RSA with SHA-256) signature
    /// </summary>
    private bool VerifyRS256Signature(CoseKey coseKey, byte[] signedData, byte[] signature)
    {
        try
        {
            if (coseKey.X == null || coseKey.Y == null)
                return false;

            using var rsa = RSA.Create(new RSAParameters
            {
                Modulus = coseKey.X,
                Exponent = coseKey.Y
            });

            return rsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RS256 signature verification failed");
            return false;
        }
    }

    /// <summary>
    /// Verify EdDSA signature (placeholder - requires additional library)
    /// </summary>
    private bool VerifyEdDSASignature(CoseKey coseKey, byte[] signedData, byte[] signature)
    {
        // EdDSA is not natively supported in .NET, would need a library like BouncyCastle
        _logger.LogWarning("EdDSA signature verification not implemented");
        return false;
    }

    #endregion

    #region Helper Classes

    private class ClientData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("challenge")]
        public string Challenge { get; set; } = string.Empty;

        [JsonPropertyName("origin")]
        public string Origin { get; set; } = string.Empty;

        [JsonPropertyName("crossOrigin")]
        public bool CrossOrigin { get; set; }
    }

    private class AuthenticatorAttestationData
    {
        public byte[] RpIdHash { get; set; } = Array.Empty<byte>();
        public bool UserPresent { get; set; }
        public bool UserVerified { get; set; }
        public uint SignCount { get; set; }
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    }

    private class CoseKey
    {
        public int KeyType { get; set; }
        public int Algorithm { get; set; }
        public int Curve { get; set; }
        public byte[]? X { get; set; }
        public byte[]? Y { get; set; }
    }

    #endregion
}
