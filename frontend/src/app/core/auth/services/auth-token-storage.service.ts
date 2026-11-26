import { Injectable } from '@angular/core';

const ACCESS_TOKEN_KEY = 'h2l.auth.accessToken';
const REFRESH_TOKEN_KEY = 'h2l.auth.refreshToken';

@Injectable({
  providedIn: 'root',
})
export class AuthTokenStorageService {
  private memoryAccessToken: string | null = null;
  private memoryRefreshToken: string | null = null;

  getAccessToken(): string | null {
    const stored = this.readSessionValue(ACCESS_TOKEN_KEY);
    return stored ?? this.memoryAccessToken;
  }

  getRefreshToken(): string | null {
    const stored = this.readSessionValue(REFRESH_TOKEN_KEY);
    return stored ?? this.memoryRefreshToken;
  }

  setTokens(accessToken: string, refreshToken: string): void {
    this.memoryAccessToken = accessToken;
    this.memoryRefreshToken = refreshToken;
    this.writeSessionValue(ACCESS_TOKEN_KEY, accessToken);
    this.writeSessionValue(REFRESH_TOKEN_KEY, refreshToken);
  }

  clearTokens(): void {
    this.memoryAccessToken = null;
    this.memoryRefreshToken = null;
    this.removeSessionValue(ACCESS_TOKEN_KEY);
    this.removeSessionValue(REFRESH_TOKEN_KEY);
  }

  private readSessionValue(key: string): string | null {
    try {
      return globalThis.sessionStorage?.getItem(key) ?? null;
    } catch {
      return null;
    }
  }

  private writeSessionValue(key: string, value: string): void {
    try {
      globalThis.sessionStorage?.setItem(key, value);
    } catch {
      // In-memory fallback already populated.
    }
  }

  private removeSessionValue(key: string): void {
    try {
      globalThis.sessionStorage?.removeItem(key);
    } catch {
      // In-memory fallback already cleared.
    }
  }
}
