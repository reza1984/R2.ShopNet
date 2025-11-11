# Passkey Implementation Guide

## Overview

This guide covers the complete passkey (WebAuthn/FIDO2) authentication implementation in R2.ShopNet, including both backend API and Angular frontend.

## Architecture

### Backend (ASP.NET Core 10)

The passkey implementation uses ASP.NET Core 10's built-in passkey support:

1. **Identity Schema Version 3**: Required for passkey support
   - Configured in `Program.cs`: `options.Stores.SchemaVersion = IdentitySchemaVersions.Version3`
   - Creates `AspNetUserPasskeys` table via EF Core migration

2. **SignInManager Methods**:
   - `MakePasskeyCreationOptionsAsync()`: Generates WebAuthn registration options
   - `PerformPasskeyAttestationAsync()`: Validates and stores passkey credentials
   - `PasskeySignInAsync()`: Authenticates users with passkeys

3. **API Endpoints** (in `PasskeyController.cs`):
   - `POST /passkey/register/begin`: Initiates passkey registration
   - `POST /passkey/register/complete`: Completes passkey registration
   - `GET /passkey/list`: Lists user's passkeys
   - `DELETE /passkey/{id}`: Removes a passkey
   - `POST /passkey/login/begin`: Initiates passkey authentication
   - `POST /passkey/login/complete`: Completes passkey authentication

### Frontend (Angular 19)

1. **PasskeyService** (`core/services/passkey.service.ts`):
   - Handles WebAuthn API interactions
   - Converts between base64url and binary formats
   - Manages passkey CRUD operations

2. **Security Settings Component** (`features/settings/security/`):
   - UI for passkey management
   - Registration, listing, and deletion
   - Platform authenticator detection

## Database Schema

### Migration: `AddPasskeySupport`

Creates the `AspNetUserPasskeys` table:

```sql
CREATE TABLE identity."AspNetUserPasskeys" (
    "CredentialId" bytea NOT NULL,
    "UserId" uuid NOT NULL,
    "Data" jsonb NOT NULL,
    CONSTRAINT "PK_AspNetUserPasskeys" PRIMARY KEY ("CredentialId"),
    CONSTRAINT "FK_AspNetUserPasskeys_Users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES identity."Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetUserPasskeys_UserId" 
    ON identity."AspNetUserPasskeys" ("UserId");
```

## API Flow

### Passkey Registration

#### 1. Begin Registration

**Request:**
```http
POST https://localhost:5000/passkey/register/begin?friendlyName=Mac%20Touch%20ID
Authorization: Bearer {access_token}
```

**Response:**
```json
{
  "registrationOptionsJson": "{\"rp\":{\"name\":\"localhost\",\"id\":\"localhost\"},\"user\":{\"id\":\"base64url\",\"name\":\"admin@shopnet.com\",\"displayName\":\"System Administrator\"},\"challenge\":\"base64url\",\"pubKeyCredParams\":[{\"type\":\"public-key\",\"alg\":-7}],\"timeout\":300000,\"excludeCredentials\":[],\"authenticatorSelection\":{\"residentKey\":\"preferred\",\"requireResidentKey\":false,\"userVerification\":\"required\"}}",
  "challenge": "ZTit1s7Gw6KTkjyRI9c06_rbCQu1wafO9Lpyeedk0_Q1lrZFdm-E-l_pUTm3rMJJh-tYE2BZBftnDE0Ql923yQ",
  "message": "Use your device's biometric authentication to register your passkey."
}
```

#### 2. Client Creates Credential

The client uses the WebAuthn API:

```typescript
const options = JSON.parse(response.registrationOptionsJson);
const credential = await navigator.credentials.create({
  publicKey: {
    challenge: base64urlToBuffer(options.challenge),
    rp: options.rp,
    user: {
      id: base64urlToBuffer(options.user.id),
      name: options.user.name,
      displayName: options.user.displayName
    },
    // ... other options
  }
});
```

#### 3. Complete Registration

**Request:**
```http
POST https://localhost:5000/passkey/register/complete
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "attestationResponseJson": "{\"id\":\"...\",\"rawId\":\"...\",\"type\":\"public-key\",\"response\":{\"clientDataJSON\":\"...\",\"attestationObject\":\"...\"}}",
  "friendlyName": "Mac Touch ID"
}
```

**Response:**
```json
{
  "success": true,
  "credentialId": "base64url-credential-id",
  "message": "Passkey registered successfully"
}
```

### Passkey Authentication

#### 1. Begin Authentication

**Request:**
```http
POST https://localhost:5000/passkey/login/begin
Content-Type: application/json

{
  "email": "admin@shopnet.com"
}
```

**Response:**
```json
{
  "assertionOptionsJson": "{\"challenge\":\"base64url\",\"timeout\":300000,\"rpId\":\"localhost\",\"allowCredentials\":[{\"type\":\"public-key\",\"id\":\"base64url\"}],\"userVerification\":\"required\"}",
  "challenge": "base64url-challenge"
}
```

#### 2. Client Gets Assertion

```typescript
const options = JSON.parse(response.assertionOptionsJson);
const assertion = await navigator.credentials.get({
  publicKey: {
    challenge: base64urlToBuffer(options.challenge),
    allowCredentials: options.allowCredentials.map(c => ({
      type: c.type,
      id: base64urlToBuffer(c.id)
    })),
    // ... other options
  }
});
```

#### 3. Complete Authentication

**Request:**
```http
POST https://localhost:5000/passkey/login/complete
Content-Type: application/json

{
  "assertionResponseJson": "{\"id\":\"...\",\"rawId\":\"...\",\"type\":\"public-key\",\"response\":{\"clientDataJSON\":\"...\",\"authenticatorData\":\"...\",\"signature\":\"...\",\"userHandle\":\"...\"}}",
  "email": "admin@shopnet.com"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

## Angular Implementation

### Service Usage

```typescript
import { PasskeyService } from '@core/services/passkey.service';

// Register a passkey
this.passkeyService.registerPasskey('Mac Touch ID').subscribe({
  next: (response) => console.log('Passkey registered:', response),
  error: (error) => console.error('Registration failed:', error)
});

// List user's passkeys
this.passkeyService.getUserPasskeys().subscribe({
  next: (passkeys) => console.log('User passkeys:', passkeys),
  error: (error) => console.error('Failed to load passkeys:', error)
});

// Delete a passkey
this.passkeyService.deletePasskey(passkeyId).subscribe({
  next: () => console.log('Passkey deleted'),
  error: (error) => console.error('Failed to delete passkey:', error)
});
```

### Component Integration

The `SecuritySettingsComponent` provides a complete UI:

```typescript
// Navigate to security settings
<a routerLink="/settings/security">Security Settings</a>

// Or programmatically
this.router.navigate(['/settings/security']);
```

### Browser Support Check

```typescript
// Check if WebAuthn is supported
if (this.passkeyService.isWebAuthnSupported()) {
  // Check for platform authenticator (Touch ID, Face ID, Windows Hello)
  const hasAuthenticator = await this.passkeyService.isPlatformAuthenticatorAvailable();
  
  if (hasAuthenticator) {
    // Show passkey registration option
  }
}
```

## Testing

### 1. Using cURL (Backend Only)

```bash
# Get access token
curl -k -X POST https://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin@shopnet.com&password=Admin123!&client_id=admin-web&scope=openid profile email roles api admin offline_access"

# Begin passkey registration
curl -k -X POST \
  -H "Authorization: Bearer {access_token}" \
  'https://localhost:5000/passkey/register/begin?friendlyName=Test%20Key'

# Response will contain WebAuthn options
```

### 2. Using Angular App

1. **Start the application:**
   ```bash
   cd /Volumes/Secure/Projects/R2.ShopNet
   aspire run
   ```

2. **Navigate to the admin app:**
   - URL: `https://localhost:7041` (or the port shown in Aspire dashboard)
   - Login with: `admin@shopnet.com` / `Admin123!`

3. **Access Security Settings:**
   - Click on profile/settings in the top-right
   - Navigate to "Security" tab
   - Click "Add Passkey" button

4. **Register a passkey:**
   - Enter a friendly name (e.g., "MacBook Touch ID")
   - Your browser will prompt for biometric authentication
   - After successful registration, the passkey will appear in the list

5. **Test passkey authentication:**
   - Logout
   - On login page, click "Sign in with Passkey"
   - Your browser will prompt for biometric authentication
   - You'll be logged in without entering a password

## Troubleshooting

### Common Issues

1. **"NotAllowedError: The operation is not allowed"**
   - Cause: User cancelled the prompt or timeout
   - Solution: Try again, ensure biometric authentication is set up

2. **"NotSupportedError: WebAuthn is not supported"**
   - Cause: Browser doesn't support WebAuthn
   - Solution: Use Chrome 67+, Firefox 60+, Safari 13+, or Edge 18+

3. **"SecurityError: The operation is insecure"**
   - Cause: Not using HTTPS (except localhost)
   - Solution: Ensure you're using HTTPS or localhost

4. **401 Unauthorized**
   - Cause: Invalid or expired access token
   - Solution: Get a fresh token using `/connect/token`

5. **400 Bad Request: "DbContext does not include IdentityUserPasskey"**
   - Cause: Identity schema version not set to Version3
   - Solution: Ensure `options.Stores.SchemaVersion = IdentitySchemaVersions.Version3` is set
   - Run migration: `dotnet ef database update`

### Debugging

Enable detailed logging in `Program.cs`:

```csharp
builder.Logging.AddFilter("Microsoft.AspNetCore.Identity", LogLevel.Debug);
```

Check browser console for WebAuthn errors:

```javascript
// In browser console
navigator.credentials.create({...}).catch(err => console.error(err));
```

## Security Considerations

1. **HTTPS Required**: WebAuthn only works over HTTPS (except localhost)
2. **User Verification**: Set to "required" for better security
3. **Attestation**: Use "none" for privacy, "direct" for enterprise
4. **Timeout**: 5 minutes (300000ms) gives users enough time
5. **Resident Keys**: Set to "preferred" for better UX
6. **Cross-Origin**: WebAuthn enforces same-origin policy

## Browser Compatibility

| Browser | Version | Platform Authenticator | Security Keys |
|---------|---------|------------------------|---------------|
| Chrome  | 67+     | ✅ (Android, Windows)  | ✅            |
| Firefox | 60+     | ✅ (Windows)           | ✅            |
| Safari  | 13+     | ✅ (macOS, iOS)        | ✅            |
| Edge    | 18+     | ✅ (Windows)           | ✅            |

## References

- [ASP.NET Core Passkeys Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-10.0)
- [WebAuthn Specification](https://www.w3.org/TR/webauthn-2/)
- [FIDO Alliance](https://fidoalliance.org/)
- [WebAuthn Guide](https://webauthn.guide/)
