import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, type CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  // ...existing code...

  const authService = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  // During SSR, always allow access to prevent hydration mismatch
  // The actual auth check will happen on the client side
  if (!isPlatformBrowser(platformId)) {
  // ...existing code...
    return true;
  }

  const isAuth = authService.isAuthenticated();
  // ...existing code...

  if (isAuth) {
  // ...existing code...
    return true;
  }

  // Store the attempted URL for redirecting after login
  const returnUrl = state.url;
  // ...existing code...

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
