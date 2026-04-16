import { HttpInterceptorFn, HttpResponse } from "@angular/common/http";
import { inject } from "@angular/core";
import { map } from "rxjs";
import { SanitizationService } from "../services/sanitization.service";

/**
 * Sanitization Interceptor - Automatic request/response sanitization
 *
 * Features:
 * - Sanitizes request bodies before sending to backend
 * - Sanitizes API responses before returning to components
 * - Works transparently with all HTTP calls
 * - Skips non-JSON content
 */
export const sanitizationInterceptor: HttpInterceptorFn = (req, next) => {
  const sanitizer = inject(SanitizationService);

  // Check if this is a JSON request that needs sanitization
  const shouldSanitizeRequest =
    (req.method === "POST" || req.method === "PUT" || req.method === "PATCH") &&
    req.headers.get("content-type")?.includes("application/json") &&
    req.body;

  // Sanitize request body if needed
  const sanitizedRequest = shouldSanitizeRequest
    ? req.clone({
        body: sanitizer.sanitizeRequestBody(req.body),
      })
    : req;

  return next(sanitizedRequest).pipe(
    map((event) => {
      // Sanitize response bodies
      if (event instanceof HttpResponse && event.body) {
        const contentType = event.headers.get("content-type") || "";

        // Only sanitize JSON responses
        if (contentType.includes("application/json")) {
          const sanitizedBody = sanitizer.sanitizeResponse(event.body);
          return event.clone({ body: sanitizedBody });
        }
      }

      return event;
    }),
  );
};
