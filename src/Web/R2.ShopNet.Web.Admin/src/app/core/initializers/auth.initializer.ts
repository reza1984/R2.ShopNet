import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from '../services/auth.service';

/**
 * Auth initializer to load authentication state before app starts
 * This prevents the flash of login page on refresh
 */
export function initializeAuth() {
  const platformId = inject(PLATFORM_ID);
  const authService = inject(AuthService);

  return () => {
    // Only run in browser (skip during SSR)
    if (isPlatformBrowser(platformId)) {
      console.log('🎬 [AuthInitializer] Initializing authentication state...');
      // The AuthService constructor already loads from localStorage
      // This just ensures it happens before routing
      return Promise.resolve();
    }
    return Promise.resolve();
  };
}
