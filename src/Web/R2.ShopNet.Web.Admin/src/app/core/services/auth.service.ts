import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { LoginRequest, LoginResponse, AuthState, UserInfo } from '../models/auth.model';
import { environment } from '../../../environments/environment.development';
import { TokenStorageService } from './token-storage.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  // Use direct Identity service URL for token endpoint (to avoid Aspire port conflict)
  // For production, this should go through the gateway
  private readonly apiUrl = environment.apiUrl;
  private readonly tokenEndpoint = `${this.apiUrl}/connect/token`;
  private readonly endSessionEndpoint = `${this.apiUrl}/connect/endsession`;
  private readonly authApiEndpoint = `${this.apiUrl}/api/auth`;

  // Auth state signals
  private authState = signal<AuthState>({
    isAuthenticated: false,
    accessToken: null,
    refreshToken: null,
    tokenExpiry: null,
    user: null
  });

  // Store id_token for logout (OIDC requires this for proper logout)
  private idToken: string | null = null;

  // Public computed signals
  public isAuthenticated = computed(() => this.authState().isAuthenticated);
  public currentUser = computed(() => this.authState().user);
  public accessToken = computed(() => this.authState().accessToken);

  constructor() {
    // Migrate old localStorage tokens (one-time security upgrade)
    this.tokenStorage.migrateFromLocalStorage();

    // Load auth state from secure storage
    this.loadAuthStateFromStorage();
  }

  /**
   * Login with username and password
   */
  login(username: string, password: string): Observable<LoginResponse> {
    const body = new URLSearchParams();
    body.set('username', username);
    body.set('password', password);
    body.set('grant_type', 'password');
    body.set('client_id', 'admin-web');
    body.set('scope', 'openid profile email roles api admin offline_access');

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });


    return this.http.post<LoginResponse>(this.tokenEndpoint, body.toString(), { headers }).pipe(
      tap(response => {
        this.handleLoginSuccess(response);
      }),
      catchError(error => {
        console.error('❌ [AuthService] Login failed!');
        console.error('📛 [AuthService] Error status:', error.status);
        console.error('📛 [AuthService] Error message:', error.message);
        console.error('📛 [AuthService] Error details:', error);
        if (error.error) {
          console.error('📛 [AuthService] Server error response:', error.error);
        }
        return throwError(() => error);
      })
    );
  }

  /**
   * Refresh the access token using refresh token
   */
  refreshToken(): Observable<LoginResponse> {
    const refreshToken = this.authState().refreshToken;

    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    const body = new URLSearchParams();
    body.set('grant_type', 'refresh_token');
    body.set('refresh_token', refreshToken);
    body.set('client_id', 'admin-web');

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    return this.http.post<LoginResponse>(this.tokenEndpoint, body.toString(), { headers }).pipe(
      tap(response => {
        this.handleLoginSuccess(response);
      }),
      catchError(error => {
        console.error('Token refresh failed:', error);
        this.logout();
        return throwError(() => error);
      })
    );
  }

  /**
   * Logout and clear all auth data
   * 
   * Note: For SPAs using token-based auth (no cookies), we don't need to call
   * the OIDC /connect/endsession endpoint. Just clear local tokens and redirect.
   * 
   * Future enhancement: Call a custom /api/auth/revoke endpoint to revoke refresh tokens
   */
  logout(): void {
    // Clear secure storage and state
    this.tokenStorage.clearAll();
    if (this.isBrowser) {
      localStorage.removeItem('r2_id_token');
    }
    this.idToken = null;
    this.authState.set({
      isAuthenticated: false,
      accessToken: null,
      refreshToken: null,
      tokenExpiry: null,
      user: null
    });

    // Redirect to login page
    this.router.navigate(['/login']);
  }

  /**
   * Check if token is expired or about to expire
   */
  isTokenExpired(): boolean {
    const expiry = this.authState().tokenExpiry;
    if (!expiry) return true;

    // Consider token expired 1 minute before actual expiry
    const expiryWithBuffer = new Date(expiry.getTime() - 60000);
    return new Date() >= expiryWithBuffer;
  }

  /**
   * Get the current access token
   */
  getAccessToken(): string | null {
    return this.authState().accessToken;
  }

  /**
   * Handle successful login response
   */
  private handleLoginSuccess(response: LoginResponse): void {
    const tokenExpiry = new Date(Date.now() + response.expires_in * 1000);
    // Parse the id_token (contains user claims), not the access_token (which is encrypted)
    const userInfo = response.id_token ? this.parseJwtToken(response.id_token) : null;

    // Store id_token for logout
    this.idToken = response.id_token || null;

    // Save to localStorage (SSO - shared across all tabs)
    this.tokenStorage.setAccessToken(response.access_token);
    this.tokenStorage.setTokenExpiry(tokenExpiry);

    if (response.refresh_token) {
      this.tokenStorage.setRefreshToken(response.refresh_token);
    }

    if (response.id_token) {
      if (this.isBrowser) {
        localStorage.setItem('r2_id_token', response.id_token);
      }
    }

    if (userInfo) {
      this.tokenStorage.setUserInfo(userInfo);
    }

    // Update state
    this.authState.set({
      isAuthenticated: true,
      accessToken: response.access_token,
      refreshToken: response.refresh_token || null,
      tokenExpiry: tokenExpiry,
      user: userInfo
    });

  }

  /**
   * Load auth state from secure storage on app init
   */
  private loadAuthStateFromStorage(): void {

    // Only access storage in browser environment
    if (!this.isBrowser) {
      return;
    }

    const accessToken = this.tokenStorage.getAccessToken();
    const refreshToken = this.tokenStorage.getRefreshToken();
    const tokenExpiry = this.tokenStorage.getTokenExpiry();
    const userInfo = this.tokenStorage.getUserInfo();
    const idToken = this.isBrowser ? localStorage.getItem('r2_id_token') : null;

    // Store id_token in memory for logout
    this.idToken = idToken;

    // Restore session if we have access token and expiry (SSO mode)
    if (accessToken && tokenExpiry) {
      this.authState.set({
        isAuthenticated: true,
        accessToken,
        refreshToken,
        tokenExpiry,
        user: userInfo
      });

      // Check if token is expired and try to refresh
      if (new Date() >= tokenExpiry && refreshToken) {
        this.refreshToken().subscribe({
          next: () => {
            console.log('✅ [AuthService] Token refreshed successfully');
          },
          error: () => {
            console.log('❌ [AuthService] Token refresh failed, logging out');
            this.logout();
          }
        });
      } else if (new Date() >= tokenExpiry && !refreshToken) {
        console.log('⏰ [AuthService] Token expired and no refresh token, logging out');
        this.logout();
      }
    } else {
      console.log('ℹ️  [AuthService] No valid session data found in localStorage');
    }
  }

  /**
   * Parse JWT token to extract user info
   */
  private parseJwtToken(token: string): UserInfo | null {
    try {
      const base64Url = token.split('.')[1];
      if (!base64Url) {
        return null;
      }

      // Convert base64url to base64
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');

      // Decode base64 string to UTF-8 string
      // Using a safer method that handles Unicode characters properly
      const jsonPayload = decodeURIComponent(
        Array.prototype.map.call(atob(base64), (c: string) => {
          return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join('')
      );

      const payload = JSON.parse(jsonPayload);

      return {
        sub: payload.sub || payload.user_id,
        email: payload.email,
        name: payload.name || payload.preferred_username,
        preferred_username: payload.preferred_username,
        first_name: payload.first_name,
        last_name: payload.last_name,
        email_verified: payload.email_verified === 'true' || payload.email_verified === true,
        roles: Array.isArray(payload.role) ? payload.role : payload.role ? [payload.role] : []
      };
    } catch (error) {
      console.error('Failed to parse JWT token:', error);
      return null;
    }
  }

  /**
   * Request password reset link via email
   */
  forgotPassword(email: string): Observable<{ message: string; email: string }> {
    return this.http.post<{ message: string; email: string }>(
      `${this.authApiEndpoint}/forgot-password`,
      { email }
    ).pipe(
      catchError(error => {
        console.error('❌ [AuthService] Forgot password request failed', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Reset password with token
   */
  resetPassword(
    email: string,
    token: string,
    newPassword: string,
    confirmPassword: string
  ): Observable<{ message: string; email: string }> {
    return this.http.post<{ message: string; email: string }>(
      `${this.authApiEndpoint}/reset-password`,
      { email, token, newPassword, confirmPassword }
    ).pipe(
      catchError(error => {
        console.error('❌ [AuthService] Reset password failed', error);
        return throwError(() => error);
      })
    );
  }
}
