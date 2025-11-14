import { Injectable, inject, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, from, switchMap, throwError, catchError } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import {
  Passkey,
  RegisterPasskeyResponse,
  CompletePasskeyRegistrationRequest,
  CompletePasskeyRegistrationResponse,
  BeginPasskeyLoginRequest,
  BeginPasskeyLoginResponse,
  CompletePasskeyLoginRequest,
  CompletePasskeyLoginResponse
} from '../models/passkey.model';
import { LoginResponse } from '../models/auth.model';

/**
 * Service for managing passkey (WebAuthn) operations
 * Implements WebAuthn Level 2 specification
 * @see https://www.w3.org/TR/webauthn-2/
 */
@Injectable({
  providedIn: 'root'
})
export class PasskeyService {
  private readonly http = inject(HttpClient);
  private readonly isBrowser: boolean;
  private readonly apiUrl = `${environment.apiUrl}/passkey`;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    this.isBrowser = isPlatformBrowser(this.platformId);
  }

  /**
   * Get list of user's passkeys
   */
  getUserPasskeys(): Observable<Passkey[]> {
    return this.http.get<Passkey[]>(`${this.apiUrl}/credentials`);
  }

  /**
   * Begin passkey registration process
   */
  beginRegistration(): Observable<RegisterPasskeyResponse> {
    return this.http.post<RegisterPasskeyResponse>(`${this.apiUrl}/register/begin`, {});
  }

  /**
   * Complete passkey registration
   */
  completeRegistration(request: CompletePasskeyRegistrationRequest): Observable<CompletePasskeyRegistrationResponse> {
    return this.http.post<CompletePasskeyRegistrationResponse>(
      `${this.apiUrl}/register/complete`,
      request
    );
  }

  /**
   * Delete a passkey
   */
  deletePasskey(passkeyId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/credentials/${passkeyId}`);
  }

  /**
   * Register a new passkey using WebAuthn API
   */
  registerPasskey(friendlyName: string): Observable<CompletePasskeyRegistrationResponse> {
    return this.beginRegistration().pipe(
      switchMap(response => {
        // Parse the registration options JSON from the backend
        const optionsData = JSON.parse(response.registrationOptionsJson);

        // Convert base64url strings to Uint8Array for WebAuthn API
        const challengeBuffer = this.base64urlToBuffer(optionsData.challenge).buffer as ArrayBuffer;
        const userIdBuffer = this.base64urlToBuffer(optionsData.user.id).buffer as ArrayBuffer;

        const options: CredentialCreationOptions = {
          publicKey: {
            challenge: challengeBuffer,
            rp: optionsData.rp,
            user: {
              id: userIdBuffer,
              name: optionsData.user.name,
              displayName: optionsData.user.displayName
            },
            pubKeyCredParams: optionsData.pubKeyCredParams,
            timeout: optionsData.timeout,
            attestation: optionsData.attestation as AttestationConveyancePreference || 'none',
            excludeCredentials: optionsData.excludeCredentials?.map((cred: any) => ({
              type: cred.type,
              id: this.base64urlToBuffer(cred.id).buffer as ArrayBuffer
            })) || [],
            authenticatorSelection: optionsData.authenticatorSelection || {
              authenticatorAttachment: 'platform',
              residentKey: 'preferred',
              userVerification: 'required'
            }
          }
        };

        // Call WebAuthn API to create credential
        return from(navigator.credentials.create(options)).pipe(
          switchMap(credential => {
            if (!credential) {
              throw new Error('Failed to create credential');
            }

            // Convert credential to JSON for backend
            const attestationResponse = this.credentialToJSON(credential as PublicKeyCredential);

            // Complete registration on backend (send as 'response' object)
            return this.completeRegistration({
              deviceName: friendlyName,
              response: attestationResponse
            });
          })
        );
      })
    );
  }

  /**
   * Convert base64url string to ArrayBuffer (RFC 4648 Section 5)
   * Used for challenge, credential ID, etc.
   */
  private base64urlToBuffer(base64url: string): Uint8Array {
    if (!this.isBrowser) {
      throw new Error('Buffer operations are only supported in browser environment');
    }

    // Replace base64url characters with standard base64
    let base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');

    // Add padding
    const padding = base64.length % 4;
    if (padding === 2) {
      base64 += '==';
    } else if (padding === 3) {
      base64 += '=';
    }

    // Decode base64 to binary string
    const binary = atob(base64);

    // Convert binary string to Uint8Array
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }

    return bytes;
  }

  /**
   * Convert ArrayBuffer to base64url string (RFC 4648 Section 5)
   * This is the proper format for WebAuthn
   */
  private bufferToBase64url(buffer: ArrayBuffer | Uint8Array): string {
    if (!this.isBrowser) {
      throw new Error('Buffer operations are only supported in browser environment');
    }

    const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.length; i++) {
      binary += String.fromCharCode(bytes[i]);
    }

    // Convert to base64 then to base64url
    return btoa(binary)
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, ''); // Remove padding
  }

  /**
   * Convert ArrayBuffer to standard base64 string
   * Used for attestationObject and clientDataJSON (backend expects standard base64)
   */
  private bufferToBase64(buffer: ArrayBuffer | Uint8Array): string {
    if (!this.isBrowser) {
      throw new Error('Buffer operations are only supported in browser environment');
    }

    const bytes = buffer instanceof Uint8Array ? buffer : new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.length; i++) {
      binary += String.fromCharCode(bytes[i]);
    }

    return btoa(binary);
  }

  /**
   * Convert PublicKeyCredential to JSON format for backend (registration)
   */
  private credentialToJSON(credential: PublicKeyCredential): any {
    const response = credential.response as AuthenticatorAttestationResponse;

    return {
      id: credential.id,
      rawId: this.bufferToBase64url(credential.rawId),
      type: credential.type,
      response: {
        clientDataJSON: this.bufferToBase64(response.clientDataJSON),
        attestationObject: this.bufferToBase64(response.attestationObject)
      },
      clientExtensionResults: credential.getClientExtensionResults() || {}
    };
  }

  /**
   * Check if WebAuthn is supported in this browser
   */
  isWebAuthnSupported(): boolean {
    if (!this.isBrowser) {
      return false;
    }

    return !!(
      window.PublicKeyCredential &&
      navigator.credentials &&
      typeof navigator.credentials.create === 'function' &&
      typeof navigator.credentials.get === 'function'
    );
  }

  /**
   * Check if platform authenticator (e.g., Touch ID, Face ID, Windows Hello) is available
   */
  async isPlatformAuthenticatorAvailable(): Promise<boolean> {
    if (!this.isWebAuthnSupported()) {
      return false;
    }

    try {
      return await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
    } catch (error) {
      console.error('Failed to check platform authenticator availability:', error);
      return false;
    }
  }

  /**
   * Check if conditional mediation (autofill UI) is available
   */
  async isConditionalMediationAvailable(): Promise<boolean> {
    if (!this.isWebAuthnSupported()) {
      return false;
    }

    try {
      return await PublicKeyCredential.isConditionalMediationAvailable?.() ?? false;
    } catch (error) {
      console.error('Failed to check conditional mediation availability:', error);
      return false;
    }
  }

  /**
   * Begin passkey login process
   */
  beginLogin(username: string): Observable<BeginPasskeyLoginResponse> {
    // Backend expects username for passkey authentication
    return this.http.post<BeginPasskeyLoginResponse>(
      `${this.apiUrl}/authenticate/begin`,
      { username }
    );
  }

  /**
   * Complete passkey login (send assertion fields directly to /connect/token)
   */
  completeLogin(assertion: any, email: string): Observable<CompletePasskeyLoginResponse> {
    // Flatten assertion object for form encoding
    const form: Record<string, string> = {
      grant_type: 'urn:ietf:params:oauth:grant-type:passkey',
      email,
      assertion: JSON.stringify(assertion),
      scope: 'openid profile email roles api admin offline_access'
    };
    // Encode as x-www-form-urlencoded
    const body = Object.entries(form)
      .map(([k, v]) => encodeURIComponent(k) + '=' + encodeURIComponent(v))
      .join('&');
    return this.http.post<CompletePasskeyLoginResponse>(
      `${environment.apiUrl}/connect/token`,
      body,
      {
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
      }
    );
  }

  /**
   * Login with passkey using WebAuthn API
   */
  loginWithPasskey(email: string): Observable<CompletePasskeyLoginResponse> {
    return this.beginLogin(email).pipe(
      switchMap(response => {
        // Convert base64url strings to Uint8Array for WebAuthn API
        const challengeBuffer = this.base64urlToBuffer(response.challenge).buffer as ArrayBuffer;
        const allowCredentials = response.allowCredentials?.map((cred: any) => ({
          type: cred.type,
          id: this.base64urlToBuffer(cred.id).buffer as ArrayBuffer
        })) || [];
        const options: CredentialRequestOptions = {
          publicKey: {
            challenge: challengeBuffer,
            allowCredentials: allowCredentials,
            timeout: response.timeout,
            rpId: response.rpId,
            userVerification: response.userVerification as UserVerificationRequirement || 'required'
          }
        };
        // Call WebAuthn API to get assertion
        return from(navigator.credentials.get(options)).pipe(
          switchMap(credential => {
            if (!credential) {
              throw new Error('Failed to get credential');
            }
            // Convert credential to JSON for backend
            const assertionResponse = this.assertionToJSON(credential as PublicKeyCredential);
            // Send assertion fields directly to backend
            return this.completeLogin(assertionResponse, email);
          })
        );
      })
    );
  }

  /**
   * Convert PublicKeyCredential assertion to JSON format for backend (authentication)
   */
  private assertionToJSON(credential: PublicKeyCredential): any {
    const response = credential.response as AuthenticatorAssertionResponse;

    // Convert ES256 signature from DER to IEEE P1363 format for .NET compatibility
    const signatureIeee = this.convertDerSignatureToIeeeP1363(new Uint8Array(response.signature));

    return {
      id: credential.id,
      rawId: this.bufferToBase64url(credential.rawId),
      type: credential.type,
      response: {
        clientDataJSON: this.bufferToBase64(response.clientDataJSON),
        authenticatorData: this.bufferToBase64(response.authenticatorData),
        signature: this.bufferToBase64(signatureIeee),
        userHandle: response.userHandle ? this.bufferToBase64url(response.userHandle) : null
      },
      clientExtensionResults: credential.getClientExtensionResults() || {}
    };
  }

  /**
   * Convert DER-encoded ECDSA signature to IEEE P1363 format
   * WebAuthn returns ES256 signatures in DER format, but .NET expects IEEE P1363
   */
  private convertDerSignatureToIeeeP1363(derSignature: Uint8Array): Uint8Array {
    try {
      // DER format: 0x30 [total-length] 0x02 [R-length] [R] 0x02 [S-length] [S]
      if (derSignature.length < 8 || derSignature[0] !== 0x30) {
        // Already in IEEE P1363 format or invalid
        return derSignature;
      }

      let offset = 2; // Skip 0x30 and total length

      // Read R
      if (derSignature[offset] !== 0x02) {
        return derSignature;
      }

      offset++;
      const rLength = derSignature[offset++];
      let r = derSignature.slice(offset, offset + rLength);
      offset += rLength;

      // Read S
      if (derSignature[offset] !== 0x02) {
        return derSignature;
      }

      offset++;
      const sLength = derSignature[offset++];
      let s = derSignature.slice(offset, offset + sLength);

      // Remove leading zero bytes if present (used for sign bit in DER)
      const rCleaned = this.removeLeadingZeros(r);
      const sCleaned = this.removeLeadingZeros(s);

      // IEEE P1363 format: R (32 bytes) || S (32 bytes) for P-256
      const ieeeSignature = new Uint8Array(64);

      // Pad with leading zeros if needed
      ieeeSignature.set(new Uint8Array(rCleaned), 32 - rCleaned.length);
      ieeeSignature.set(new Uint8Array(sCleaned), 64 - sCleaned.length);

      return ieeeSignature;
    } catch (error) {
      console.error('Failed to convert DER signature to IEEE P1363:', error);
      return derSignature; // Return original if conversion fails
    }
  }

  /**
   * Remove leading zero bytes from a Uint8Array
   */
  private removeLeadingZeros(data: Uint8Array): Uint8Array {
    let firstNonZero = 0;
    while (firstNonZero < data.length && data[firstNonZero] === 0) {
      firstNonZero++;
    }

    if (firstNonZero === 0) {
      return data;
    }

    return data.slice(firstNonZero);
  }

  /**
   * Get user-friendly error message from WebAuthn error
   */
  getErrorMessage(error: any): string {
    if (error instanceof DOMException) {
      switch (error.name) {
        case 'NotAllowedError':
          return 'Authentication was cancelled or timed out. Please try again.';
        case 'InvalidStateError':
          return 'This passkey is already registered.';
        case 'NotSupportedError':
          return 'Passkeys are not supported on this device or browser.';
        case 'SecurityError':
          return 'Security error occurred. Please ensure you are on a secure connection.';
        case 'AbortError':
          return 'The operation was aborted.';
        case 'ConstraintError':
          return 'The passkey does not meet the requirements.';
        case 'UnknownError':
          return 'An unknown error occurred. Please try again.';
        default:
          return `Authentication error: ${error.message}`;
      }
    }

    if (error?.status === 404) {
      return 'No passkey found for this account.';
    }

    if (error?.status === 400) {
      return 'Invalid passkey authentication.';
    }

    if (error?.status === 0) {
      return 'Unable to connect to server. Please check your connection.';
    }

    return error?.message || 'An unexpected error occurred. Please try again.';
  }
}
