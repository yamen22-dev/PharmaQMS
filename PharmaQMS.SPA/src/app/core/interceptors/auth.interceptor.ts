import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { SKIP_AUTH } from './auth.tokens';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/refresh') || req.url.includes('/auth/revoke');
  const shouldSkip = req.context.get(SKIP_AUTH) || isAuthEndpoint;

  const accessToken = shouldSkip ? null : authService.getAccessToken();
  const authedRequest = accessToken
    ? req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
    : req;

  return next(authedRequest).pipe(
    catchError(error => {
      if (shouldSkip || error?.status !== 401) {
        return throwError(() => error);
      }

      return authService.refreshSession().pipe(
        switchMap(session => next(req.clone({ setHeaders: { Authorization: `Bearer ${session.accessToken}` } }))),
        catchError(refreshError => {
          authService.clearSession();
          return throwError(() => refreshError);
        })
      );
    })
  );
};
