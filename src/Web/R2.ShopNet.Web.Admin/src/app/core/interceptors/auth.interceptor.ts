import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Skip adding auth header for token endpoint requests
  if (req.url.includes('/connect/token')) {
    return next(req);
  }

  // Get the access token
  const token = authService.getAccessToken();

  // If no token, proceed without authorization header
  if (!token) {
    return next(req);
  }

  // Check if token is expired
  if (authService.isTokenExpired()) {
    // Try to refresh the token
    return authService.refreshToken().pipe(
      switchMap(() => {
        // Get the new token after refresh
        const newToken = authService.getAccessToken();
        const clonedReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${newToken}`
          }
        });
        return next(clonedReq);
      }),
      catchError((error) => {
        // If refresh fails, logout and propagate error
        authService.logout();
        return throwError(() => error);
      })
    );
  }

  // Clone the request and add the authorization header
  const clonedReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(clonedReq).pipe(
    catchError((error) => {
      // If 401 Unauthorized, try to refresh token
      if (error.status === 401 && !req.url.includes('/connect/token')) {
        return authService.refreshToken().pipe(
          switchMap(() => {
            const newToken = authService.getAccessToken();
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${newToken}`
              }
            });
            return next(retryReq);
          }),
          catchError((refreshError) => {
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  );
};
