import { HttpClient, HttpContext } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import {
  BehaviorSubject,
  catchError,
  finalize,
  map,
  Observable,
  of,
  shareReplay,
  tap,
  throwError,
} from "rxjs";
import { environment } from "../../../environments/environment";
import { AuthResponse } from "../models/auth-response.model";
import { LoginRequest } from "../models/login-request.model";
import { RefreshTokenRequest } from "../models/refresh-token-request.model";
import { AuthStorageService } from "./auth-storage.service";
import { SKIP_AUTH } from "../interceptors/auth.tokens";

@Injectable({ providedIn: "root" })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(AuthStorageService);
  private readonly sessionSubject = new BehaviorSubject<AuthResponse | null>(
    this.storage.readSession(),
  );
  private readonly displayNameCache = new Map<string, string>();
  private readonly displayNameRequests = new Map<string, Observable<string>>();

  readonly session$ = this.sessionSubject.asObservable();

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(tap((session) => this.setSession(session)));
  }

  refreshSession(): Observable<AuthResponse> {
    const refreshToken = this.storage.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error("Missing refresh token."));
    }

    const body: RefreshTokenRequest = { refreshToken };
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, body, {
        context: new HttpContext().set(SKIP_AUTH, true),
      })
      .pipe(tap((session) => this.setSession(session)));
  }

  ensureSession(): Observable<boolean> {
    if (this.storage.hasValidAccessToken()) {
      return of(true);
    }

    if (!this.storage.getRefreshToken()) {
      return of(false);
    }

    return this.refreshSession().pipe(
      map(() => true),
      catchError(() => {
        this.clearSession();
        return of(false);
      }),
    );
  }

  logout(): Observable<void> {
    const refreshToken = this.storage.getRefreshToken();
    if (!refreshToken) {
      this.clearSession();
      return of(void 0);
    }

    const body: RefreshTokenRequest = { refreshToken };
    return this.http
      .post<void>(`${environment.apiBaseUrl}/auth/revoke`, body, {
        context: new HttpContext().set(SKIP_AUTH, true),
      })
      .pipe(
        catchError(() => of(void 0)),
        tap(() => this.clearSession()),
        map(() => void 0),
      );
  }

  getAccessToken(): string | null {
    return this.storage.getAccessToken();
  }

  getSession(): AuthResponse | null {
    return this.storage.readSession();
  }

  hasRole(role: string): boolean {
    const session = this.getSession();
    return Array.isArray(session?.roles) && session.roles.includes(role);
  }

  clearSession(): void {
    this.storage.clearSession();
    this.sessionSubject.next(null);
  }

  private setSession(session: AuthResponse): void {
    this.storage.saveSession(session);
    this.sessionSubject.next(session);
  }

  getUserNameById(userId: string): Observable<string> {
    const normalizedUserId = userId.trim();
    if (!normalizedUserId) {
      return of("Onbekende gebruiker");
    }

    const cachedName = this.displayNameCache.get(normalizedUserId);
    if (cachedName) {
      return of(cachedName);
    }

    const inFlightRequest = this.displayNameRequests.get(normalizedUserId);
    if (inFlightRequest) {
      return inFlightRequest;
    }

    const request$ = this.http
      .post<any>(
        `${environment.apiBaseUrl}/auth/display-name`,
        { userId: normalizedUserId },
        { context: new HttpContext().set(SKIP_AUTH, true) },
      )
      .pipe(
        map((response) => {
          // backend might return DisplayName or displayName depending on serializer
          return (
            response?.displayName ??
            response?.DisplayName ??
            "Onbekende gebruiker"
          );
        }),
        tap((displayName) => {
          if (displayName !== "Onbekende gebruiker") {
            this.displayNameCache.set(normalizedUserId, displayName);
          }
        }),
        catchError(() => of("Onbekende gebruiker")),
        finalize(() => {
          this.displayNameRequests.delete(normalizedUserId);
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );

    this.displayNameRequests.set(normalizedUserId, request$);
    return request$;
  }
}
