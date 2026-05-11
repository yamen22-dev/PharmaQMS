import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { catchError, map, of } from "rxjs";
import { AuthService } from "../services/auth.service";

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.ensureSession().pipe(
    map((isAuthenticated) =>
      isAuthenticated ? router.createUrlTree(["/dashboard"]) : true,
    ),
    catchError(() => of(true)),
  );
};
