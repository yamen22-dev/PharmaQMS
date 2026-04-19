import { Injectable } from '@angular/core';
import { AuthResponse } from '../models/auth-response.model';

interface JwtPayload {
  exp?: number;
  [key: string]: unknown;
}

@Injectable({ providedIn: 'root' })
export class AuthStorageService {
  private readonly storageKey = 'pharmaqms.auth.session';

  readSession(): AuthResponse | null {
    const rawSession = sessionStorage.getItem(this.storageKey);
    if (!rawSession) {
      return null;
    }

    try {
      return JSON.parse(rawSession) as AuthResponse;
    } catch {
      sessionStorage.removeItem(this.storageKey);
      return null;
    }
  }

  saveSession(session: AuthResponse): void {
    sessionStorage.setItem(this.storageKey, JSON.stringify(session));
  }

  clearSession(): void {
    sessionStorage.removeItem(this.storageKey);
  }

  getAccessToken(): string | null {
    return this.readSession()?.accessToken ?? null;
  }

  getRefreshToken(): string | null {
    return this.readSession()?.refreshToken ?? null;
  }

  hasValidAccessToken(): boolean {
    const accessToken = this.getAccessToken();
    if (!accessToken) {
      return false;
    }

    const payload = this.decodeJwt(accessToken);
    return typeof payload?.exp === 'number' && payload.exp * 1000 > Date.now();
  }

  getSessionDisplayName(): string {
    const session = this.readSession();
    if (!session) {
      return 'Guest';
    }

    return `${session.firstName} ${session.lastName}`.trim();
  }

  private decodeJwt(token: string): JwtPayload | null {
    const segments = token.split('.');
    if (segments.length < 2) {
      return null;
    }

    try {
      const payload = this.base64UrlDecode(segments[1]);
      return JSON.parse(payload) as JwtPayload;
    } catch {
      return null;
    }
  }

  private base64UrlDecode(value: string): string {
    const base64 = value.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    return atob(padded);
  }
}
