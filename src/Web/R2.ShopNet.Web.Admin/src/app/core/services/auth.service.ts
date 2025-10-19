import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { LoginRequest, LoginResponse, AuthState, UserInfo } from '../models/auth.model';
import { environment } from '../../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  // Use direct Identity service URL for token endpoint (to avoid Aspire port conflict)
  // For production, this should go through the gateway
  private readonly apiUrl = environment.apiUrl;
  private readonly tokenEndpoint = `${this.apiUrl}/connect/token`;

  // Storage keys
  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';
  private readonly TOKEN_EXPIRY_KEY = 'token_expiry';
  private readonly USER_INFO_KEY = 'user_info';

  // Auth state signals
  private authState = signal<AuthState>({
    isAuthenticated: false,
    accessToken: null,
    refreshToken: null,
    tokenExpiry: null,
    user: null
  });

  // Public computed signals
  public isAuthenticated = computed(() => this.authState().isAuthenticated);
  public currentUser = computed(() => this.authState().user);
  public accessToken = computed(() => this.authState().accessToken);

  constructor() {
    console.log('🚀 [AuthService] Initializing AuthService...');
    console.log('🌍 [AuthService] Platform:', this.isBrowser ? 'Browser' : 'Server');
    console.log('🔗 [AuthService] API URL:', this.apiUrl);
    console.log('🔗 [AuthService] Token Endpoint:', this.tokenEndpoint);
    this.loadAuthStateFromStorage();
    console.log('🔐 [AuthService] Initial auth state - Authenticated:', this.isAuthenticated());
  }

  /**
   * Login with username and password
   */
  login(username: string, password: string): Observable<LoginResponse> {
    console.log('🔐 [AuthService] Starting login process...');
    console.log('📧 [AuthService] Username:', username);
    console.log('🌐 [AuthService] Token endpoint:', this.tokenEndpoint);

    const body = new URLSearchParams();
    body.set('username', username);
    body.set('password', password);
    body.set('grant_type', 'password');
    body.set('client_id', 'admin-web');
    body.set('scope', 'openid profile email roles api admin offline_access');

    console.log('📦 [AuthService] Request body:', body.toString());

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    console.log('📤 [AuthService] Sending POST request to token endpoint...');

    return this.http.post<LoginResponse>(this.tokenEndpoint, body.toString(), { headers }).pipe(
      tap(response => {
        console.log('✅ [AuthService] Login successful! Response:', response);
        console.log('🎫 [AuthService] Access token received:', response.access_token ? 'YES' : 'NO');
        console.log('🔄 [AuthService] Refresh token received:', response.refresh_token ? 'YES' : 'NO');
        console.log('⏱️  [AuthService] Token expires in:', response.expires_in, 'seconds');
        this.handleLoginSuccess(response);
        console.log('💾 [AuthService] Auth state updated. Authenticated:', this.isAuthenticated());
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
   */
  logout(): void {
    // Clear storage (only in browser)
    if (this.isBrowser) {
      localStorage.removeItem(this.ACCESS_TOKEN_KEY);
      localStorage.removeItem(this.REFRESH_TOKEN_KEY);
      localStorage.removeItem(this.TOKEN_EXPIRY_KEY);
      localStorage.removeItem(this.USER_INFO_KEY);
    }

    // Reset state
    this.authState.set({
      isAuthenticated: false,
      accessToken: null,
      refreshToken: null,
      tokenExpiry: null,
      user: null
    });

    // Redirect to login
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

    // Save to localStorage (only in browser)
    if (this.isBrowser) {
      localStorage.setItem(this.ACCESS_TOKEN_KEY, response.access_token);
      if (response.refresh_token) {
        localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refresh_token);
      }
      localStorage.setItem(this.TOKEN_EXPIRY_KEY, tokenExpiry.toISOString());
      if (userInfo) {
        localStorage.setItem(this.USER_INFO_KEY, JSON.stringify(userInfo));
      }
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
   * Load auth state from localStorage on app init
   */
  private loadAuthStateFromStorage(): void {
    console.log('💾 [AuthService] Loading auth state from storage...');

    // Only access localStorage in browser environment
    if (!this.isBrowser) {
      console.log('⚠️  [AuthService] Not in browser, skipping localStorage');
      return;
    }

    const accessToken = localStorage.getItem(this.ACCESS_TOKEN_KEY);
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    const tokenExpiryStr = localStorage.getItem(this.TOKEN_EXPIRY_KEY);
    const userInfoStr = localStorage.getItem(this.USER_INFO_KEY);

    console.log('🔍 [AuthService] Tokens found in localStorage:', {
      accessToken: accessToken ? 'YES' : 'NO',
      refreshToken: refreshToken ? 'YES' : 'NO',
      tokenExpiry: tokenExpiryStr ? 'YES' : 'NO',
      userInfo: userInfoStr ? 'YES' : 'NO'
    });

    // Restore session if we have at least an access token and expiry
    if (accessToken && tokenExpiryStr) {
      const tokenExpiry = new Date(tokenExpiryStr);
      const userInfo = userInfoStr ? JSON.parse(userInfoStr) : null;

      console.log('⏰ [AuthService] Token expiry:', tokenExpiry);
      console.log('⏰ [AuthService] Current time:', new Date());
      console.log('✅ [AuthService] Token valid:', new Date() < tokenExpiry);

      // Always restore auth state if we have tokens (even if expired)
      // This prevents redirect to login on page refresh
      console.log('✅ [AuthService] Restoring authenticated session');
      this.authState.set({
        isAuthenticated: true,
        accessToken,
        refreshToken: refreshToken || null,
        tokenExpiry,
        user: userInfo
      });

      // If token is expired and we have a refresh token, try to refresh
      if (new Date() >= tokenExpiry && refreshToken) {
        console.log('⏰ [AuthService] Token expired, attempting refresh in background...');
        this.refreshToken().subscribe({
          next: () => {
            console.log('✅ [AuthService] Token refreshed successfully');
          },
          error: () => {
            console.log('❌ [AuthService] Token refresh failed, logging out');
            // Refresh failed, clear everything
            this.logout();
          }
        });
      } else if (new Date() >= tokenExpiry && !refreshToken) {
        console.log('⚠️  [AuthService] Token expired but no refresh token available. User will need to re-login.');
      }
    } else {
      console.log('ℹ️  [AuthService] No valid tokens found in storage');
    }
  }

  /**
   * Parse JWT token to extract user info
   */
  private parseJwtToken(token: string): UserInfo | null {
    try {
      const base64Url = token.split('.')[1];
      if (!base64Url) {
        console.error('Invalid JWT token format');
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
}
