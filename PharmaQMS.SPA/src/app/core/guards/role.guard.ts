import { CanActivateFn, Router } from "@angular/router";
import { inject } from "@angular/core";
import { map } from "rxjs";
import { AuthService } from "../services/auth.service";

export function roleGuard(
  allowedRoles: string[],
  redirectTo: string = "/dashboard",
): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.session$.pipe(
      map((session) => {
        if (!session) {
          return router.createUrlTree(["/login"]);
        }

        const hasRole =
          Array.isArray(session.roles) &&
          session.roles.some((role) => allowedRoles.includes(role));

        if (!hasRole) {
          return router.createUrlTree([redirectTo]);
        }

        return true;
      }),
    );
  };
}