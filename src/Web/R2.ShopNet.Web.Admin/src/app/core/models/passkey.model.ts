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
    type: string;
    alg: number;
  }>;
  timeout: number;
  attestation: string;
  authenticatorSelection?: {
    authenticatorAttachment?: string;
    requireResidentKey?: boolean;
    userVerification?: string;
  };
}

export interface RegisterPasskeyResponse {
  options: RegisterPasskeyOptions;
  sessionId: string;
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
