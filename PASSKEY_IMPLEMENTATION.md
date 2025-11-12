# Passkey Implementation Guide for R2.ShopNet Identity Service

## Overview

This document outlines the complete implementation of passkey (WebAuthn) authentication in the R2.ShopNet Identity service using .NET 10's native passkey support integrated with OpenIddict OAuth 2.0/OpenID Connect.

**Technology Stack:**
- ASP.NET Core 10 native WebAuthn APIs (no FIDO2 library required)
- OpenIddict 5.8.0 for OAuth2/OIDC
- PostgreSQL for credential storage
- ASP.NET Core Identity for user management
- Angular 20 frontend with WebAuthn browser APIs

---

## Architecture Decision

### Why .NET 10 Native Support?

**Advantages:**
- ✅ Built directly into ASP.NET Core Identity
- ✅ No external FIDO2 library dependencies
- ✅ Seamless integration with existing Identity infrastructure
- ✅ Microsoft-maintained and supported
- ✅ Simpler implementation for standard authentication scenarios

**Limitations:**
- ⚠️ Scoped to common authentication scenarios only
- ⚠️ Not a general-purpose WebAuthn library
- ⚠️ Limited public API surface

**Alternative Considered:**
- `fido2-net-lib` - Full WebAuthn implementation (rejected per requirement: "dont use fido")

### Controller Responsibilities

**AuthorizationController** (`/connect/token`) - **All Token Issuance**
- ✅ Password grant flow (existing)
- ✅ Refresh token grant flow (existing)
- ✅ **Passkey grant flow (NEW)** - Token issuance via passkey authentication
- Purpose: OAuth2/OIDC token endpoint

**PasskeyController** (`/api/passkey/*`) - **Credential Management**
- ✅ Passkey registration (begin/complete)
- ✅ Passkey management (list credentials, delete credentials)
- Purpose: WebAuthn credential lifecycle (not token issuance)

**AuthController** (`/api/auth/*`) - **Traditional Authentication**
- ✅ User registration (existing)
- ✅ Password-based login (existing - returns JWT, not OpenIddict token)
- ✅ Forgot/reset password (existing)
- Purpose: Traditional authentication and account management

### Why This Architecture?

1. **OAuth2 Compliance** - All token issuance through standard `/connect/token` endpoint
2. **Clean Separation** - Authentication flows vs. credential management
3. **Consistent Tokens** - Passkey generates identical RSA-signed OpenIddict tokens as password flow
4. **Extensible** - Easy to add more grant types in the future
5. **Follows Best Practices** - Matches OpenIddict and OAuth2 conventions

---

## Token Generation - Identical for Password and Passkey

### Key Principle

**Both password and passkey flows generate the exact same RSA-signed OpenIddict tokens.**

The only difference is **how we verify the user's identity**:
- Password flow: Validates password via `SignInManager.CheckPasswordSignInAsync()`
- Passkey flow: Validates WebAuthn assertion via `PasskeyService.CompleteAuthenticationAsync()`

After verification, both flows:
1. Call the same `CreateClaimsPrincipalAsync(user, scopes)` method
2. Return the same `SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)`
3. OpenIddict generates the same RSA-signed JWT tokens with same claims

### Token Structure (Identical)

Both flows produce tokens with the exact same structure:

```json
{
  "header": {
    "alg": "RS256",              // RSA signature algorithm
    "kid": "...",                 // Key ID from development certificate
    "typ": "at+jwt"
  },
  "payload": {
    "sub": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "name": "John Doe",
    "preferred_username": "user@example.com",
    "user_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "first_name": "John",
    "last_name": "Doe",
    "email_verified": "True",
    "role": ["User", "Admin"],
    "scope": ["openid", "profile", "email", "roles", "api"],
    "iss": "https://localhost:5001",
    "aud": "resource-server",
    "exp": 1699999999,
    "iat": 1699996399
  },
  "signature": "..."             // RSA signature using development certificate
}
```

---

## Implementation Phases

---

## Phase 1: Database Schema

### 1.1 Create PasskeyCredential Entity

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Domain/Entities/PasskeyCredential.cs`

```csharp
namespace R2.ShopNet.Identity.Domain.Entities;

public class PasskeyCredential
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public byte[] CredentialId { get; set; } = null!; // Raw credential ID from WebAuthn
    public byte[] PublicKey { get; set; } = null!; // COSE-encoded public key
    public uint SignCount { get; set; } // Counter for replay protection
    public Guid? AaGuid { get; set; } // Authenticator attestation GUID
    public string? DeviceName { get; set; } // User-friendly name
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}
```

### 1.2 Update ApplicationUser Entity

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Domain/Entities/ApplicationUser.cs`

Add navigation property:
```csharp
public ICollection<PasskeyCredential> PasskeyCredentials { get; set; } = new List<PasskeyCredential>();
```

### 1.3 Update IdentityDbContext

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Infrastructure/Persistence/IdentityDbContext.cs`

Add DbSet:
```csharp
public DbSet<PasskeyCredential> PasskeyCredentials { get; set; }
```

Configure entity in `OnModelCreating`:
```csharp
modelBuilder.Entity<PasskeyCredential>(entity =>
{
    entity.ToTable("PasskeyCredentials", "identity");
    entity.HasKey(e => e.Id);

    entity.Property(e => e.CredentialId)
        .IsRequired()
        .HasMaxLength(1024);

    entity.Property(e => e.PublicKey)
        .IsRequired();

    entity.Property(e => e.DeviceName)
        .HasMaxLength(100);

    entity.HasIndex(e => e.CredentialId)
        .IsUnique();

    entity.HasOne(e => e.User)
        .WithMany(u => u.PasskeyCredentials)
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

### 1.4 Create Migration

```bash
dotnet ef migrations add AddPasskeyCredentials \
  --startup-project ../R2.ShopNet.Identity.API/R2.ShopNet.Identity.API.csproj \
  --context IdentityDbContext
```

---

## Phase 2: Service Layer

### 2.1 Create IPasskeyService Interface

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/Interfaces/IPasskeyService.cs`

```csharp
namespace R2.ShopNet.Identity.Application.Interfaces;

public interface IPasskeyService
{
    // Registration flow
    Task<PasskeyRegistrationOptions> BeginRegistrationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PasskeyRegistrationResult> CompleteRegistrationAsync(Guid userId, PasskeyRegistrationResponse response, CancellationToken cancellationToken = default);

    // Authentication flow
    Task<PasskeyAuthenticationOptions> BeginAuthenticationAsync(string? username = null, CancellationToken cancellationToken = default);
    Task<PasskeyAuthenticationResult> CompleteAuthenticationAsync(PasskeyAuthenticationResponse response, CancellationToken cancellationToken = default);

    // Credential management
    Task<IEnumerable<PasskeyCredentialDto>> GetUserCredentialsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteCredentialAsync(Guid userId, Guid credentialId, CancellationToken cancellationToken = default);
}
```

### 2.2 Create DTOs

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/DTOs/Passkey/PasskeyRegistrationDtos.cs`

```csharp
namespace R2.ShopNet.Identity.Application.DTOs.Passkey;

public class PasskeyRegistrationOptions
{
    public string Challenge { get; set; } = string.Empty;
    public string RpId { get; set; } = string.Empty;
    public string RpName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public int Timeout { get; set; } = 60000;
    public string Attestation { get; set; } = "none";
    public List<PublicKeyCredentialParameters> PubKeyCredParams { get; set; } = new();
    public AuthenticatorSelectionCriteria AuthenticatorSelection { get; set; } = new();
    public List<PublicKeyCredentialDescriptor> ExcludeCredentials { get; set; } = new();
}

public class PublicKeyCredentialParameters
{
    public string Type { get; set; } = "public-key";
    public int Alg { get; set; } // COSE algorithm identifier
}

public class AuthenticatorSelectionCriteria
{
    public string? AuthenticatorAttachment { get; set; } // "platform" or "cross-platform"
    public string ResidentKey { get; set; } = "preferred";
    public bool RequireResidentKey { get; set; } = false;
    public string UserVerification { get; set; } = "preferred";
}

public class PublicKeyCredentialDescriptor
{
    public string Type { get; set; } = "public-key";
    public string Id { get; set; } = string.Empty;
    public List<string>? Transports { get; set; }
}

public class PasskeyRegistrationResponse
{
    public string Id { get; set; } = string.Empty;
    public string RawId { get; set; } = string.Empty;
    public string Type { get; set; } = "public-key";
    public AuthenticatorAttestationResponse Response { get; set; } = new();
}

public class AuthenticatorAttestationResponse
{
    public string ClientDataJSON { get; set; } = string.Empty;
    public string AttestationObject { get; set; } = string.Empty;
}

public class PasskeyRegistrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CredentialId { get; set; }
}
```

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/DTOs/Passkey/PasskeyAuthenticationDtos.cs`

```csharp
namespace R2.ShopNet.Identity.Application.DTOs.Passkey;

public class PasskeyAuthenticationOptions
{
    public string Challenge { get; set; } = string.Empty;
    public string RpId { get; set; } = string.Empty;
    public int Timeout { get; set; } = 60000;
    public List<PublicKeyCredentialDescriptor> AllowCredentials { get; set; } = new();
    public string UserVerification { get; set; } = "preferred";
}

public class PasskeyAuthenticationResponse
{
    public string Id { get; set; } = string.Empty;
    public string RawId { get; set; } = string.Empty;
    public string Type { get; set; } = "public-key";
    public AuthenticatorAssertionResponse Response { get; set; } = new();
}

public class AuthenticatorAssertionResponse
{
    public string ClientDataJSON { get; set; } = string.Empty;
    public string AuthenticatorData { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string? UserHandle { get; set; }
}

public class PasskeyAuthenticationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
}

public class PasskeyCredentialDto
{
    public Guid Id { get; set; }
    public string? DeviceName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
```

### 2.3 Implement PasskeyService

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Infrastructure/Services/PasskeyService.cs`

```csharp
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
        // Implementation using .NET 10 native WebAuthn APIs
        // TODO: Implement using ASP.NET Core Identity passkey APIs
        throw new NotImplementedException();
    }

    public async Task<PasskeyRegistrationResult> CompleteRegistrationAsync(Guid userId, PasskeyRegistrationResponse response, CancellationToken cancellationToken = default)
    {
        // Implementation using .NET 10 native WebAuthn APIs
        // TODO: Implement using ASP.NET Core Identity passkey APIs
        throw new NotImplementedException();
    }

    public async Task<PasskeyAuthenticationOptions> BeginAuthenticationAsync(string? username = null, CancellationToken cancellationToken = default)
    {
        // Implementation using .NET 10 native WebAuthn APIs
        // TODO: Implement using ASP.NET Core Identity passkey APIs
        throw new NotImplementedException();
    }

    public async Task<PasskeyAuthenticationResult> CompleteAuthenticationAsync(PasskeyAuthenticationResponse response, CancellationToken cancellationToken = default)
    {
        // Implementation using .NET 10 native WebAuthn APIs
        // TODO: Implement using ASP.NET Core Identity passkey APIs
        throw new NotImplementedException();
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
```

---

## Phase 3: CQRS Commands and Queries

### 3.1 Register Passkey Command

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/Commands/Passkey/RegisterPasskey/RegisterPasskeyCommand.cs`

```csharp
namespace R2.ShopNet.Identity.Application.Commands.Passkey.RegisterPasskey;

public record RegisterPasskeyCommand(
    Guid UserId,
    PasskeyRegistrationResponse Response,
    string? DeviceName = null
) : ICommand<Result<Guid>>;
```

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/Commands/Passkey/RegisterPasskey/RegisterPasskeyCommandHandler.cs`

```csharp
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
            request.Response,
            cancellationToken);

        if (!result.Success)
        {
            return Result<Guid>.Failure(result.ErrorMessage ?? "Passkey registration failed");
        }

        return Result<Guid>.Success(result.CredentialId!.Value);
    }
}
```

### 3.2 List User Passkeys Query

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/Queries/Passkey/GetUserPasskeys/GetUserPasskeysQuery.cs`

```csharp
namespace R2.ShopNet.Identity.Application.Queries.Passkey.GetUserPasskeys;

public record GetUserPasskeysQuery(Guid UserId) : IQuery<Result<IEnumerable<PasskeyCredentialDto>>>;
```

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/Queries/Passkey/GetUserPasskeys/GetUserPasskeysQueryHandler.cs`

```csharp
namespace R2.ShopNet.Identity.Application.Queries.Passkey.GetUserPasskeys;

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
        return Result<IEnumerable<PasskeyCredentialDto>>.Success(credentials);
    }
}
```

### 3.3 Delete Passkey Command

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/Commands/Passkey/DeletePasskey/DeletePasskeyCommand.cs`

```csharp
namespace R2.ShopNet.Identity.Application.Commands.Passkey.DeletePasskey;

public record DeletePasskeyCommand(Guid UserId, Guid CredentialId) : ICommand<Result<bool>>;
```

**File:** `/src/Services/Identity/R2.ShopNet.Identity.Application/Commands/Passkey/DeletePasskey/DeletePasskeyCommandHandler.cs`

```csharp
namespace R2.ShopNet.Identity.Application.Commands.Passkey.DeletePasskey;

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
            return Result<bool>.Failure("Passkey credential not found or already deleted");
        }

        return Result<bool>.Success(true);
    }
}
```

---

## Phase 4: API Controllers

### 4.1 Update AuthorizationController - Token Issuance

**File:** `/src/Services/Identity/R2.ShopNet.Identity.API/Controllers/AuthorizationController.cs`

**Add IPasskeyService dependency:**

```csharp
public class AuthorizationController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IPasskeyService _passkeyService; // ADD THIS

    public AuthorizationController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IPasskeyService passkeyService) // ADD THIS
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _passkeyService = passkeyService; // ADD THIS
    }
}
```

**Update Exchange method to handle passkey grant:**

```csharp
[HttpPost("~/connect/token")]
[Produces("application/json")]
public async Task<IActionResult> Exchange()
{
    var request = HttpContext.GetOpenIddictServerRequest() ??
        throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

    if (request.IsPasswordGrantType())
    {
        return await HandlePasswordFlowAsync(request);
    }

    if (request.IsRefreshTokenGrantType())
    {
        return await HandleRefreshTokenFlowAsync(request);
    }

    // ADD: Handle passkey grant type
    if (request.GrantType == "urn:ietf:params:oauth:grant-type:passkey")
    {
        return await HandlePasskeyFlowAsync(request);
    }

    return BadRequest(new OpenIddictResponse
    {
        Error = Errors.UnsupportedGrantType,
        ErrorDescription = "The specified grant type is not supported."
    });
}
```

**Add HandlePasskeyFlowAsync method:**

```csharp
private async Task<IActionResult> HandlePasskeyFlowAsync(OpenIddictRequest request)
{
    // Extract passkey assertion from request
    var assertionJson = request.GetParameter("assertion")?.ToString();

    if (string.IsNullOrEmpty(assertionJson))
    {
        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.InvalidRequest,
            ErrorDescription = "The passkey assertion is missing."
        });
    }

    // Deserialize the WebAuthn assertion response
    PasskeyAuthenticationResponse? assertion;
    try
    {
        assertion = System.Text.Json.JsonSerializer.Deserialize<PasskeyAuthenticationResponse>(assertionJson);
        if (assertion == null)
        {
            throw new InvalidOperationException("Failed to deserialize assertion.");
        }
    }
    catch (Exception ex)
    {
        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.InvalidRequest,
            ErrorDescription = $"Invalid assertion format: {ex.Message}"
        });
    }

    // Verify the passkey assertion using PasskeyService
    var result = await _passkeyService.CompleteAuthenticationAsync(assertion);

    if (!result.Success || result.UserId == null)
    {
        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.InvalidGrant,
            ErrorDescription = result.ErrorMessage ?? "Passkey authentication failed."
        });
    }

    // Find the user
    var user = await _userManager.FindByIdAsync(result.UserId.Value.ToString());

    if (user == null)
    {
        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.InvalidGrant,
            ErrorDescription = "The user account no longer exists."
        });
    }

    // Check if user is active
    if (!user.IsActive)
    {
        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.InvalidGrant,
            ErrorDescription = "The user account is not active."
        });
    }

    // Check if user can sign in
    if (!await _signInManager.CanSignInAsync(user))
    {
        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.InvalidGrant,
            ErrorDescription = "The user is not allowed to sign in."
        });
    }

    // *** THIS IS THE KEY PART ***
    // Create the SAME claims principal as password flow
    var principal = await CreateClaimsPrincipalAsync(user, request.GetScopes());

    // Update last login timestamp
    user.LastLoginAt = DateTime.UtcNow;
    await _userManager.UpdateAsync(user);

    // *** RETURN THE EXACT SAME WAY ***
    // OpenIddict will generate the SAME RSA-signed JWT token
    return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
```

### 4.2 Create PasskeyController - Credential Management

**File:** `/src/Services/Identity/R2.ShopNet.Identity.API/Controllers/PasskeyController.cs`

```csharp
namespace R2.ShopNet.Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasskeyController : ControllerBase
{
    private readonly IPasskeyService _passkeyService;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly ILogger<PasskeyController> _logger;

    public PasskeyController(
        IPasskeyService passkeyService,
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        ILogger<PasskeyController> logger)
    {
        _passkeyService = passkeyService;
        _commandDispatcher = commandDispatcher;
        _queryDispatcher = queryDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Begin passkey registration - returns challenge and options
    /// User must be authenticated to register a passkey
    /// </summary>
    [HttpPost("register/begin")]
    [Authorize]
    [ProducesResponseType(typeof(PasskeyRegistrationOptions), StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyRegistrationOptions>> BeginRegistration()
    {
        var userId = GetCurrentUserId();
        var options = await _passkeyService.BeginRegistrationAsync(userId);
        return Ok(options);
    }

    /// <summary>
    /// Complete passkey registration - stores the credential
    /// </summary>
    [HttpPost("register/complete")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CompleteRegistration([FromBody] PasskeyRegistrationRequest request)
    {
        var userId = GetCurrentUserId();
        var command = new RegisterPasskeyCommand(userId, request.Response, request.DeviceName);
        var result = await _commandDispatcher.Dispatch(command);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.Error });

        return Ok(new { CredentialId = result.Value, Message = "Passkey registered successfully" });
    }

    /// <summary>
    /// Begin passkey authentication - returns challenge and options
    /// This is a PUBLIC endpoint (no [Authorize]) - used during login before user is authenticated
    /// </summary>
    [HttpPost("authenticate/begin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PasskeyAuthenticationOptions), StatusCodes.Status200OK)]
    public async Task<ActionResult<PasskeyAuthenticationOptions>> BeginAuthentication(
        [FromBody] BeginAuthenticationRequest? request = null)
    {
        var options = await _passkeyService.BeginAuthenticationAsync(request?.Username);
        return Ok(options);
    }

    /// <summary>
    /// List user's registered passkeys
    /// </summary>
    [HttpGet("credentials")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PasskeyCredentialDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetCredentials()
    {
        var userId = GetCurrentUserId();
        var query = new GetUserPasskeysQuery(userId);
        var result = await _queryDispatcher.Dispatch(query);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a passkey credential
    /// </summary>
    [HttpDelete("credentials/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteCredential(Guid id)
    {
        var userId = GetCurrentUserId();
        var command = new DeletePasskeyCommand(userId, id);
        var result = await _commandDispatcher.Dispatch(command);

        if (!result.IsSuccess)
            return BadRequest(new { Error = result.Error });

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(Claims.Subject)?.Value
                       ?? User.FindFirst("user_id")?.Value
                       ?? throw new UnauthorizedAccessException("User ID not found in token");

        return Guid.Parse(userIdClaim);
    }
}

public record PasskeyRegistrationRequest(
    PasskeyRegistrationResponse Response,
    string? DeviceName = null
);

public record BeginAuthenticationRequest(
    string? Username = null
);
```

---

## Phase 5: Configuration and Dependency Injection

### 5.1 Update Program.cs

**File:** `/src/Services/Identity/R2.ShopNet.Identity.API/Program.cs`

**Add custom grant type to OpenIddict configuration (around line 143):**

```csharp
// Configure OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<IdentityDbContext>()
               .ReplaceDefaultEntities<Guid>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.SetLogoutEndpointUris("/connect/endsession");

        // Enable flows
        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow()
               .AllowCustomFlow("urn:ietf:params:oauth:grant-type:passkey"); // ADD THIS LINE

        options.AcceptAnonymousClients();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.DisableAccessTokenEncryption();

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableLogoutEndpointPassthrough()
               .DisableTransportSecurityRequirement();

        options.SetAccessTokenLifetime(TimeSpan.FromHours(1))
               .SetRefreshTokenLifetime(TimeSpan.FromDays(14));
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });
```

**Register PasskeyService:**

```csharp
// Register Services
builder.Services.AddScoped<IPasskeyService, PasskeyService>();
```

### 5.2 Configuration Settings

**File:** `/src/Services/Identity/R2.ShopNet.Identity.API/appsettings.json`

```json
{
  "WebAuthn": {
    "RelyingPartyId": "localhost",
    "RelyingPartyName": "R2.ShopNet",
    "Origin": "https://localhost:4200",
    "Timeout": 60000,
    "ChallengeSize": 32
  }
}
```

**File:** `/src/Services/Identity/R2.ShopNet.Identity.API/appsettings.Production.json`

```json
{
  "WebAuthn": {
    "RelyingPartyId": "r2shopnet.com",
    "RelyingPartyName": "R2.ShopNet",
    "Origin": "https://r2shopnet.com",
    "Timeout": 60000,
    "ChallengeSize": 32
  }
}
```

---

## Phase 6: Angular Frontend Integration

### 6.1 Update Auth Service

**File:** `/src/Web/R2.ShopNet.Web/src/app/core/services/auth.service.ts`

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenEndpoint = `${environment.identityApiUrl}/connect/token`;
  private readonly passkeyEndpoint = `${environment.identityApiUrl}/api/passkey`;

  constructor(private http: HttpClient) {}

  // Existing password login
  loginWithPassword(username: string, password: string): Observable<TokenResponse> {
    const body = new URLSearchParams({
      grant_type: 'password',
      username: username,
      password: password,
      scope: 'openid profile email roles api'
    });

    return this.http.post<TokenResponse>(
      this.tokenEndpoint,
      body.toString(),
      { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
    );
  }

  // NEW: Passkey login - returns SAME token structure as password login
  async loginWithPasskey(): Promise<TokenResponse> {
    // Step 1: Get authentication challenge
    const options = await firstValueFrom(
      this.http.post<PasskeyAuthenticationOptions>(
        `${this.passkeyEndpoint}/authenticate/begin`,
        {}
      )
    );

    // Step 2: Call WebAuthn browser API
    const credential = await navigator.credentials.get({
      publicKey: this.convertAuthenticationOptions(options)
    }) as PublicKeyCredential;

    // Step 3: Encode credential
    const assertion = this.encodeAssertionResponse(credential);

    // Step 4: Exchange assertion for OpenIddict token at /connect/token
    const body = new URLSearchParams({
      grant_type: 'urn:ietf:params:oauth:grant-type:passkey',
      assertion: JSON.stringify(assertion),
      scope: 'openid profile email roles api'
    });

    return firstValueFrom(
      this.http.post<TokenResponse>(
        this.tokenEndpoint,
        body.toString(),
        { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
      )
    );
  }

  // NEW: Register passkey (user must be logged in)
  async registerPasskey(deviceName?: string): Promise<void> {
    // Step 1: Get registration challenge
    const options = await firstValueFrom(
      this.http.post<PasskeyRegistrationOptions>(
        `${this.passkeyEndpoint}/register/begin`,
        {}
      )
    );

    // Step 2: Call WebAuthn browser API
    const credential = await navigator.credentials.create({
      publicKey: this.convertRegistrationOptions(options)
    }) as PublicKeyCredential;

    // Step 3: Send credential to server
    const response = this.encodeRegistrationResponse(credential);

    await firstValueFrom(
      this.http.post(`${this.passkeyEndpoint}/register/complete`, {
        response,
        deviceName
      })
    );
  }

  private convertAuthenticationOptions(options: PasskeyAuthenticationOptions): PublicKeyCredentialRequestOptions {
    return {
      challenge: this.base64UrlDecode(options.challenge),
      rpId: options.rpId,
      timeout: options.timeout,
      userVerification: options.userVerification as UserVerificationRequirement,
      allowCredentials: options.allowCredentials?.map(c => ({
        id: this.base64UrlDecode(c.id),
        type: 'public-key' as PublicKeyCredentialType,
        transports: c.transports as AuthenticatorTransport[]
      }))
    };
  }

  private convertRegistrationOptions(options: PasskeyRegistrationOptions): PublicKeyCredentialCreationOptions {
    return {
      challenge: this.base64UrlDecode(options.challenge),
      rp: { id: options.rpId, name: options.rpName },
      user: {
        id: this.base64UrlDecode(options.userId),
        name: options.userName,
        displayName: options.userDisplayName
      },
      pubKeyCredParams: options.pubKeyCredParams,
      timeout: options.timeout,
      attestation: options.attestation as AttestationConveyancePreference,
      authenticatorSelection: options.authenticatorSelection as AuthenticatorSelectionCriteria,
      excludeCredentials: options.excludeCredentials?.map(c => ({
        id: this.base64UrlDecode(c.id),
        type: 'public-key' as PublicKeyCredentialType,
        transports: c.transports as AuthenticatorTransport[]
      }))
    };
  }

  private encodeAssertionResponse(credential: PublicKeyCredential): any {
    const response = credential.response as AuthenticatorAssertionResponse;

    return {
      id: credential.id,
      rawId: this.arrayBufferToBase64Url(credential.rawId),
      type: credential.type,
      response: {
        clientDataJSON: this.arrayBufferToBase64Url(response.clientDataJSON),
        authenticatorData: this.arrayBufferToBase64Url(response.authenticatorData),
        signature: this.arrayBufferToBase64Url(response.signature),
        userHandle: response.userHandle ? this.arrayBufferToBase64Url(response.userHandle) : null
      }
    };
  }

  private encodeRegistrationResponse(credential: PublicKeyCredential): any {
    const response = credential.response as AuthenticatorAttestationResponse;

    return {
      id: credential.id,
      rawId: this.arrayBufferToBase64Url(credential.rawId),
      type: credential.type,
      response: {
        clientDataJSON: this.arrayBufferToBase64Url(response.clientDataJSON),
        attestationObject: this.arrayBufferToBase64Url(response.attestationObject)
      }
    };
  }

  private base64UrlDecode(input: string): ArrayBuffer {
    const base64 = input.replace(/-/g, '+').replace(/_/g, '/');
    const rawData = atob(base64);
    const buffer = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; i++) {
      buffer[i] = rawData.charCodeAt(i);
    }
    return buffer.buffer;
  }

  private arrayBufferToBase64Url(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    bytes.forEach(b => binary += String.fromCharCode(b));
    return btoa(binary)
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=/g, '');
  }
}
```

### 6.2 Update Login Component

**File:** `/src/Web/R2.ShopNet.Web/src/app/features/auth/login/login.component.ts`

```typescript
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private messageService: MessageService,
    private fb: FormBuilder
  ) {
    this.loginForm = this.fb.group({
      username: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  // Existing password login
  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.isLoading = true;
    const { username, password } = this.loginForm.value;

    this.authService.loginWithPassword(username, password).subscribe({
      next: (tokens) => {
        this.authService.setTokens(tokens);
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Login Failed',
          detail: error.error?.error_description || 'Invalid credentials'
        });
        this.isLoading = false;
      }
    });
  }

  // NEW: Passkey login - gets SAME tokens as password login
  async loginWithPasskey(): Promise<void> {
    try {
      this.isLoading = true;
      const tokens = await this.authService.loginWithPasskey();
      this.authService.setTokens(tokens);
      this.router.navigate(['/dashboard']);
    } catch (error: any) {
      this.messageService.add({
        severity: 'error',
        summary: 'Passkey Login Failed',
        detail: error.error?.error_description || 'Could not authenticate with passkey'
      });
    } finally {
      this.isLoading = false;
    }
  }
}
```

---

## Authentication Flow Comparison

### Password Flow (Existing)

```
1. User enters username/password
2. Angular → POST /connect/token
   Body: grant_type=password&username=...&password=...
3. AuthorizationController.HandlePasswordFlowAsync()
   - SignInManager.CheckPasswordSignInAsync()
   - CreateClaimsPrincipalAsync()
   - SignIn() → OpenIddict token
4. Response: { access_token, refresh_token, ... }
```

### Passkey Flow (NEW)

```
1. User clicks "Login with Passkey"
2. Angular → POST /api/passkey/authenticate/begin
   Response: { challenge, rpId, allowCredentials, ... }
3. Angular → navigator.credentials.get() (WebAuthn API)
   User authenticates with biometrics
   Returns: WebAuthn assertion
4. Angular → POST /connect/token
   Body: grant_type=urn:ietf:params:oauth:grant-type:passkey&assertion={...}
5. AuthorizationController.HandlePasskeyFlowAsync()
   - PasskeyService.CompleteAuthenticationAsync()
   - CreateClaimsPrincipalAsync()
   - SignIn() → OpenIddict token (IDENTICAL to password flow)
6. Response: { access_token, refresh_token, ... } (SAME structure)
```

**Key Point:** Both flows produce identical RSA-signed JWT tokens!

---

## Security Considerations

### 1. Challenge Management
- Store challenges in memory cache with short expiration (2 minutes)
- Validate challenge on credential completion
- One-time use only - invalidate after use

### 2. Origin Validation
- Strictly validate `rpId` matches expected domain
- Verify origin in assertion responses
- Implement CORS policies correctly

### 3. Credential Storage
- Store only public keys (never private keys)
- Private keys remain on user's device
- Implement proper encryption at rest for database

### 4. Replay Attack Prevention
- Increment and validate signature counter
- Reject credentials with decremented counter
- Log suspicious activity

### 5. Rate Limiting
- Limit registration attempts per user
- Limit authentication attempts per IP
- Implement exponential backoff

### 6. User Management
- Allow users to manage multiple passkeys
- Provide device naming for easy identification
- Keep password auth as fallback option

---

## API Endpoint Summary

### Token Issuance (AuthorizationController)

| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/connect/token` | POST | Anonymous | Issue tokens (password, passkey, refresh) |
| `/connect/endsession` | POST | Authenticated | Logout |

### Passkey Management (PasskeyController)

| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/passkey/authenticate/begin` | POST | Anonymous | Get authentication challenge |
| `/api/passkey/register/begin` | POST | **Authenticated** | Get registration challenge |
| `/api/passkey/register/complete` | POST | **Authenticated** | Store credential |
| `/api/passkey/credentials` | GET | **Authenticated** | List user's passkeys |
| `/api/passkey/credentials/{id}` | DELETE | **Authenticated** | Delete passkey |

---

## Testing Strategy

### Unit Tests
- [ ] PasskeyService registration flow
- [ ] PasskeyService authentication flow
- [ ] Command handlers validation
- [ ] Entity mapping and relationships

### Integration Tests
- [ ] Full registration flow end-to-end
- [ ] Full authentication flow end-to-end
- [ ] OpenIddict token generation with passkey
- [ ] Database persistence and retrieval
- [ ] Token verification by downstream services

### Browser Compatibility Testing
- [ ] Chrome/Edge (Windows Hello, Touch ID)
- [ ] Firefox (Windows Hello, Touch ID)
- [ ] Safari (Touch ID, Face ID)
- [ ] Mobile browsers (iOS Safari, Chrome)

---

## Implementation Checklist

### Backend
- [ ] Create PasskeyCredential entity
- [ ] Update ApplicationUser with navigation property
- [ ] Update IdentityDbContext configuration
- [ ] Generate and apply migration
- [ ] Create IPasskeyService interface and DTOs
- [ ] Implement PasskeyService with .NET 10 APIs
- [ ] Add custom grant type to OpenIddict config
- [ ] Update AuthorizationController with HandlePasskeyFlowAsync
- [ ] Create PasskeyController for credential management
- [ ] Register services in Program.cs
- [ ] Create CQRS commands/queries
- [ ] Add WebAuthn configuration
- [ ] Write unit tests
- [ ] Write integration tests

### Frontend
- [ ] Update AuthService with passkey methods
- [ ] Add passkey login button to LoginComponent
- [ ] Add passkey registration to ProfileComponent
- [ ] Add passkey management UI (list/delete)
- [ ] Handle WebAuthn browser API calls
- [ ] Add error handling and user feedback
- [ ] Test browser compatibility

### DevOps
- [ ] Update environment configuration
- [ ] Configure HTTPS for development
- [ ] Set up production rpId and certificates
- [ ] Add feature flags if needed
- [ ] Update deployment documentation

---

## Migration Path

### For Existing Users
1. Users log in with username/password
2. Option to "Add Passkey" in profile settings
3. Register passkey as additional authentication method
4. Can use either password or passkey to login
5. Both authentication methods produce identical tokens

### For New Users
1. Option to create account with passkey only (no password)
2. Email verification still required
3. Can add password later if needed

### Rollback Strategy
- Password authentication remains fully functional
- Passkey is additive, not replacement
- Can disable passkey endpoints via feature flag
- Database migration is reversible

---

## Timeline Estimate

- **Phase 1 (Database):** 2-4 hours
- **Phase 2 (Service Layer):** 8-12 hours
- **Phase 3 (CQRS):** 4-6 hours
- **Phase 4 (API Controllers):** 6-8 hours
- **Phase 5 (Configuration):** 2-4 hours
- **Phase 6 (Frontend):** 8-12 hours
- **Testing:** 8-16 hours
- **Documentation & Polish:** 4-8 hours

**Total Estimated Time:** 42-70 hours (5-9 business days)

---

## Resources

### Official Documentation
- [ASP.NET Core Passkeys Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys)
- [WebAuthn Specification](https://www.w3.org/TR/webauthn-2/)
- [OpenIddict Documentation](https://documentation.openiddict.com/)

### Community Resources
- [WebAuthn Guide](https://webauthn.guide/)
- [Passkeys.dev](https://passkeys.dev/)
- [Andrew Lock's .NET 10 Passkey Guide](https://andrewlock.net/exploring-dotnet-10-preview-features-6-passkey-support-for-aspnetcore-identity/)

---

## Next Steps

1. Review and approve this implementation plan
2. Set up development environment with HTTPS
3. Start with Phase 1: Database schema
4. Proceed phase by phase with testing at each step
5. Integration testing after Phase 5
6. Frontend integration in Phase 6
7. Comprehensive testing across browsers
8. Deploy to staging environment
9. User acceptance testing
10. Production deployment with feature flag

---

**Document Version:** 2.0 (Unified)
**Last Updated:** 2025-11-12
**Author:** AI Assistant
**Status:** Implementation Guide
