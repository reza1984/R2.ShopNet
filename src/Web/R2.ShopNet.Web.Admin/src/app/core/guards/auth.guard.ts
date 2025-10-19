import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  console.log('🛡️  [AuthGuard] Checking authentication for route:', state.url);

  const authService = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  // During SSR, always allow access to prevent hydration mismatch
  // The actual auth check will happen on the client side
  if (!isPlatformBrowser(platformId)) {
    console.log('🖥️  [AuthGuard] Running on server, allowing access for SSR');
    return true;
  }

  const isAuth = authService.isAuthenticated();
  console.log('🔐 [AuthGuard] Is authenticated:', isAuth);

  if (isAuth) {
    console.log('✅ [AuthGuard] User is authenticated, allowing access');
    return true;
  }

  // Store the attempted URL for redirecting after login
  const returnUrl = state.url;
  console.log('❌ [AuthGuard] User not authenticated, redirecting to login');
  console.log('🎯 [AuthGuard] Return URL will be:', returnUrl);

  // Redirect to login page with return url
  router.navigate(['/login'], { queryParams: { returnUrl } });
  return false;
};

export const publicGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  // During SSR, allow access to public routes
  if (!isPlatformBrowser(platformId)) {
    return true;
  }

  // If already authenticated, redirect to dashboard
  if (authService.isAuthenticated()) {
    router.navigate(['/dashboard']);
    return false;
  }

  return true;
};
