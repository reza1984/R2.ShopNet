export interface Passkey {
  id: string;
  deviceName: string;
  createdAt: string;
  lastUsedAt?: string;
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
  response: {
    id: string;
    rawId: string;
    type: string;
    response: {
      clientDataJSON: string;
      attestationObject: string;
    };
    clientExtensionResults?: any;
  };
  deviceName?: string;
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
  challenge: string;
  rpId: string;
  timeout: number;
  allowCredentials: Array<{
    type: string;
    id: string;
    transports: string[] | null;
  }>;
  userVerification: string;
}

export interface CompletePasskeyLoginRequest {
  assertionResponseJson: string;
  email: string;
}

export interface CompletePasskeyLoginResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
  id_token?: string;
}
