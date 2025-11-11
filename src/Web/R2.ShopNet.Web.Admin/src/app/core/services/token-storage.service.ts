import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Secure token storage service
 * 
 * Storage Strategy (SSO-Compatible):
 * - Access Token: Stored in localStorage (shared across tabs for SSO)
 * - Refresh Token: Stored in localStorage (future: httpOnly cookie recommended)
 * - User Info: Stored in localStorage
 * - Token Expiry: Stored in localStorage
 * 
 * SSO Features:
 * - Tokens shared across all browser tabs (true SSO experience)
 * - Login once, work in multiple tabs
 * - Logout in one tab = logout in all tabs
 * - Storage events sync auth state across tabs
 * 
 * Security Features:
 * - Short-lived access tokens (refresh frequently)
 * - Tokens cleared on logout
 * - Storage events for cross-tab sync
 * - Future: httpOnly cookies for refresh tokens (backend needed)
 * 
 * Note: For maximum security, implement:
 * 1. Content Security Policy (CSP) headers
 * 2. Input sanitization to prevent XSS
 * 3. httpOnly cookies for refresh tokens (backend)
 * 4. Regular security audits
 */
@Injectable({
  providedIn: 'root'
})
export class TokenStorageService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  // Storage keys for localStorage (SSO across tabs)
  private readonly ACCESS_TOKEN_KEY = 'r2_access_token';
  private readonly REFRESH_TOKEN_KEY = 'r2_refresh_token';
  private readonly TOKEN_EXPIRY_KEY = 'r2_token_expiry';
  private readonly USER_INFO_KEY = 'r2_user_info';

  /**
   * Store access token in localStorage
   * localStorage enables SSO - shared across all tabs
   */
  setAccessToken(token: string): void {
    if (!this.isBrowser) return;
    
    try {
      localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
      console.log('🔐 [TokenStorage] Access token stored in localStorage (SSO enabled)');
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to store access token:', error);
    }
  }

  /**
   * Get access token from localStorage
   */
  getAccessToken(): string | null {
    if (!this.isBrowser) return null;

    try {
      return localStorage.getItem(this.ACCESS_TOKEN_KEY);
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to get access token:', error);
      return null;
    }
  }

  /**
   * Clear access token from localStorage
   */
  clearAccessToken(): void {
    if (!this.isBrowser) return;
    
    try {
      localStorage.removeItem(this.ACCESS_TOKEN_KEY);
      console.log('🗑️ [TokenStorage] Access token cleared from localStorage');
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to clear access token:', error);
    }
  }

  /**
   * Store refresh token in localStorage
   * localStorage enables SSO across tabs
   * Future: This should be in httpOnly cookie managed by backend
   */
  setRefreshToken(token: string): void {
    if (!this.isBrowser) return;
    
    try {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
      console.log('🔄 [TokenStorage] Refresh token stored in localStorage (SSO enabled)');
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to store refresh token:', error);
    }
  }

  /**
   * Get refresh token from localStorage
   */
  getRefreshToken(): string | null {
    if (!this.isBrowser) return null;

    try {
      return localStorage.getItem(this.REFRESH_TOKEN_KEY);
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to get refresh token:', error);
      return null;
    }
  }

  /**
   * Clear refresh token from localStorage
   */
  clearRefreshToken(): void {
    if (!this.isBrowser) return;
    
    try {
      localStorage.removeItem(this.REFRESH_TOKEN_KEY);
      console.log('🗑️ [TokenStorage] Refresh token cleared from localStorage');
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to clear refresh token:', error);
    }
  }

  /**
   * Store token expiry in localStorage (SSO)
   */
  setTokenExpiry(expiry: Date): void {
    if (!this.isBrowser) return;
    
    try {
      localStorage.setItem(this.TOKEN_EXPIRY_KEY, expiry.toISOString());
      console.log('💾 [TokenStorage] Token expiry stored in localStorage');
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to store token expiry:', error);
    }
  }

  /**
   * Get token expiry from localStorage
   */
  getTokenExpiry(): Date | null {
    if (!this.isBrowser) return null;

    try {
      const expiryStr = localStorage.getItem(this.TOKEN_EXPIRY_KEY);
      return expiryStr ? new Date(expiryStr) : null;
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to get token expiry:', error);
      return null;
    }
  }

  /**
   * Store user info in localStorage (SSO)
   */
  setUserInfo(userInfo: any): void {
    if (!this.isBrowser) return;

    try {
      localStorage.setItem(this.USER_INFO_KEY, JSON.stringify(userInfo));
      console.log('💾 [TokenStorage] User info stored in localStorage');
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to store user info:', error);
    }
  }

  /**
   * Get user info from localStorage
   */
  getUserInfo(): any | null {
    if (!this.isBrowser) return null;

    try {
      const userInfoStr = localStorage.getItem(this.USER_INFO_KEY);
      return userInfoStr ? JSON.parse(userInfoStr) : null;
    } catch (error) {
      console.error('❌ [TokenStorage] Failed to get user info:', error);
      return null;
    }
  }

  /**
   * Clear all stored authentication data
   * This will trigger storage events to sync logout across all tabs
   */
  clearAll(): void {
    // Clear all tokens
    this.clearAccessToken();
    this.clearRefreshToken();

    // Clear localStorage (only in browser)
    if (this.isBrowser) {
      try {
        localStorage.removeItem(this.TOKEN_EXPIRY_KEY);
        localStorage.removeItem(this.USER_INFO_KEY);
        console.log('🗑️ [TokenStorage] All auth data cleared from localStorage (SSO logout)');
      } catch (error) {
        console.error('❌ [TokenStorage] Failed to clear auth data:', error);
      }
    }
  }

  /**
   * Check if authentication data exists
   */
  hasAuthData(): boolean {
    return this.getAccessToken() !== null || this.getTokenExpiry() !== null;
  }

  /**
   * Migrate from old token keys to new prefixed keys (one-time migration)
   * This preserves existing sessions during upgrade
   */
  migrateFromLocalStorage(): void {
    if (!this.isBrowser) return;

    // Token migration check

    try {
      // Check for old unprefixed localStorage keys
      const oldAccessToken = localStorage.getItem('access_token');
      const oldRefreshToken = localStorage.getItem('refresh_token');
      const oldTokenExpiry = localStorage.getItem('token_expiry');
      const oldUserInfo = localStorage.getItem('user_info');

      if (oldAccessToken || oldRefreshToken || oldTokenExpiry || oldUserInfo) {
        // Migrating old tokens to new format
        
        // Migrate to new prefixed keys
        if (oldAccessToken) {
          localStorage.setItem(this.ACCESS_TOKEN_KEY, oldAccessToken);
          localStorage.removeItem('access_token');
        }
        if (oldRefreshToken) {
          localStorage.setItem(this.REFRESH_TOKEN_KEY, oldRefreshToken);
          localStorage.removeItem('refresh_token');
        }
        if (oldTokenExpiry) {
          localStorage.setItem(this.TOKEN_EXPIRY_KEY, oldTokenExpiry);
          localStorage.removeItem('token_expiry');
        }
        if (oldUserInfo) {
          localStorage.setItem(this.USER_INFO_KEY, oldUserInfo);
          localStorage.removeItem('user_info');
        }

        // Token migration complete. Session preserved.
      } else {
        // No token migration needed
      }
    } catch (error) {
      console.error('❌ [TokenStorage] Migration failed:', error);
    }
  }
}
