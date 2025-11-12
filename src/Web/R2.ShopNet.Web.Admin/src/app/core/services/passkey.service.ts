import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, from, switchMap } from 'rxjs';
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

@Injectable({
  providedIn: 'root'
})
export class PasskeyService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/passkey`;

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
   * Convert base64url string to Uint8Array with proper ArrayBuffer
   */
  private base64urlToBuffer(base64url: string): Uint8Array {
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + (4 - base64.length % 4) % 4, '=');
    const binary = atob(padded);

    // Create a new ArrayBuffer and Uint8Array to ensure proper type
    const buffer = new ArrayBuffer(binary.length);
    const bytes = new Uint8Array(buffer);

    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }

    return bytes;
  }

  /**
   * Convert Uint8Array to base64url string
   */
  private bufferToBase64url(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.length; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary); // standard base64 for backend compatibility
  }

  /**
   * Convert PublicKeyCredential to JSON format for backend
   */
  private credentialToJSON(credential: PublicKeyCredential): any {
    const response = credential.response as AuthenticatorAttestationResponse;

    // Ensure clientDataJSON and attestationObject are always ArrayBuffer before encoding

    const clientDataJSONBuffer = response.clientDataJSON instanceof ArrayBuffer
      ? response.clientDataJSON
      : typeof response.clientDataJSON === 'string'
        ? new TextEncoder().encode(response.clientDataJSON).buffer
        : new Uint8Array(response.clientDataJSON).buffer;

    const attestationObjectBuffer = response.attestationObject instanceof ArrayBuffer
      ? response.attestationObject
      : typeof response.attestationObject === 'string'
        ? new TextEncoder().encode(response.attestationObject).buffer
        : new Uint8Array(response.attestationObject).buffer;

    return {
      id: credential.id,
      rawId: this.bufferToBase64url(credential.rawId),
      type: credential.type,
      response: {
        clientDataJSON: this.bufferToBase64url(clientDataJSONBuffer),
        attestationObject: this.bufferToBase64url(attestationObjectBuffer)
      },
      clientExtensionResults: credential.getClientExtensionResults() || {}
    };
  }

  /**
   * Check if WebAuthn is supported in this browser
   */
  isWebAuthnSupported(): boolean {
    return !!(window.PublicKeyCredential && navigator.credentials && navigator.credentials.create);
  }

  /**
   * Check if platform authenticator (e.g., Touch ID, Face ID) is available
   */
  async isPlatformAuthenticatorAvailable(): Promise<boolean> {
    if (!this.isWebAuthnSupported()) {
      return false;
    }

    try {
      return await PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable();
    } catch {
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
   * Convert PublicKeyCredential assertion to JSON format for backend
   */
  private assertionToJSON(credential: PublicKeyCredential): any {
    const response = credential.response as AuthenticatorAssertionResponse;

    return {
      id: credential.id,
      rawId: this.bufferToBase64url(credential.rawId),
      type: credential.type,
      response: {
        clientDataJSON: this.bufferToBase64url(response.clientDataJSON),
        authenticatorData: this.bufferToBase64url(response.authenticatorData),
        signature: this.bufferToBase64url(response.signature),
        userHandle: response.userHandle ? this.bufferToBase64url(response.userHandle) : null
      },
      clientExtensionResults: credential.getClientExtensionResults() || {}
    };
  }
}
