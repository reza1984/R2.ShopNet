import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, from, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import {
  Passkey,
  RegisterPasskeyResponse,
  CompletePasskeyRegistrationRequest,
  CompletePasskeyRegistrationResponse
} from '../models/passkey.model';

@Injectable({
  providedIn: 'root'
})
export class PasskeyService {
  private http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/passkey`;

  /**
   * Get list of user's passkeys
   */
  getUserPasskeys(): Observable<Passkey[]> {
    return this.http.get<Passkey[]>(`${this.apiUrl}/list`);
  }

  /**
   * Begin passkey registration process
   */
  beginRegistration(friendlyName?: string): Observable<RegisterPasskeyResponse> {
    const url = friendlyName
      ? `${this.apiUrl}/register/begin?friendlyName=${encodeURIComponent(friendlyName)}`
      : `${this.apiUrl}/register/begin`;
    return this.http.post<RegisterPasskeyResponse>(url, {});
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
    return this.http.delete<void>(`${this.apiUrl}/${passkeyId}`);
  }

  /**
   * Register a new passkey using WebAuthn API
   */
  registerPasskey(friendlyName?: string): Observable<CompletePasskeyRegistrationResponse> {
    return this.beginRegistration(friendlyName).pipe(
      switchMap(response => {
        // Convert base64url to Uint8Array for WebAuthn API
        const options: CredentialCreationOptions = {
          publicKey: {
            challenge: this.base64urlToBuffer(response.options.challenge),
            rp: response.options.rp,
            user: {
              id: this.base64urlToBuffer(response.options.user.id),
              name: response.options.user.name,
              displayName: response.options.user.displayName
            },
            pubKeyCredParams: response.options.pubKeyCredParams,
            timeout: response.options.timeout,
            attestation: response.options.attestation as AttestationConveyancePreference,
            authenticatorSelection: response.options.authenticatorSelection
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

            // Complete registration on backend
            return this.completeRegistration({
              attestationResponseJson: JSON.stringify(attestationResponse),
              friendlyName
            });
          })
        );
      })
    );
  }

  /**
   * Convert base64url string to Uint8Array
   */
  private base64urlToBuffer(base64url: string): Uint8Array {
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + (4 - base64.length % 4) % 4, '=');
    const binary = atob(padded);
    const bytes = new Uint8Array(binary.length);
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
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
  }

  /**
   * Convert PublicKeyCredential to JSON format for backend
   */
  private credentialToJSON(credential: PublicKeyCredential): any {
    const response = credential.response as AuthenticatorAttestationResponse;

    return {
      id: credential.id,
      rawId: this.bufferToBase64url(credential.rawId),
      type: credential.type,
      response: {
        clientDataJSON: this.bufferToBase64url(response.clientDataJSON),
        attestationObject: this.bufferToBase64url(response.attestationObject)
      }
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
}
