export interface LoginRequest {
  username: string;
  password: string;
  grant_type: string;
  client_id: string;
  scope?: string;
}

export interface LoginResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  refresh_token?: string;
  id_token?: string;
  scope?: string;
}

export interface AuthState {
  isAuthenticated: boolean;
  accessToken: string | null;
  refreshToken: string | null;
  tokenExpiry: Date | null;
  user: UserInfo | null;
}

export interface UserInfo {
  sub: string;
  email: string;
  name: string;
  preferred_username: string;
  first_name?: string;
  last_name?: string;
  email_verified: boolean;
  roles: string[];
}
