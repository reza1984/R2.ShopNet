export interface Passkey {
  id: string;
  userId: string;
  friendlyName: string;
  credentialId: string;
  createdAt: string;
  lastUsedAt?: string;
  userAgent?: string;
  ipAddress?: string;
  isActive: boolean;
}

export interface RegisterPasskeyOptions {
  challenge: string;
  rp: {
    name: string;
    id: string;
  };
  user: {
    id: string;
    name: string;
    displayName: string;
  };
  pubKeyCredParams: Array<{
    type: 'public-key';
    alg: number;
  }>;
  timeout: number;
  attestation?: string;
  excludeCredentials?: Array<{
    type: 'public-key';
    id: string;
  }>;
  authenticatorSelection?: {
    authenticatorAttachment?: 'platform' | 'cross-platform';
    residentKey?: 'discouraged' | 'preferred' | 'required';
    requireResidentKey?: boolean;
    userVerification?: 'discouraged' | 'preferred' | 'required';
  };
}

export interface RegisterPasskeyResponse {
  registrationOptionsJson: string; // JSON string containing WebAuthn options
  challenge: string; // Base64url-encoded challenge
  message: string; // User-facing message
}

export interface CompletePasskeyRegistrationRequest {
  attestationResponseJson: string;
  friendlyName?: string;
}

export interface CompletePasskeyRegistrationResponse {
  success: boolean;
  credentialId: string;
  message: string;
}

export interface BeginPasskeyLoginRequest {
  email: string;
}

export interface BeginPasskeyLoginResponse {
  assertionOptionsJson: string; // JSON string containing WebAuthn options
  challenge: string; // Base64url-encoded challenge
}

export interface CompletePasskeyLoginRequest {
  assertionResponseJson: string;
  email: string;
}

export interface CompletePasskeyLoginResponse {
  accessToken: string;
  refreshToken: string;
  idToken: string;
  tokenType: string;
  expiresIn: number;
}
