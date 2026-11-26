import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthTokenStorageService } from './auth-token-storage.service';

describe('AuthTokenStorageService', () => {
  let service: AuthTokenStorageService;

  beforeEach(() => {
    globalThis.localStorage?.clear();
    globalThis.sessionStorage?.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthTokenStorageService);
  });

  it('stores and clears access and refresh tokens without touching localStorage', () => {
    service.setTokens('access-123', 'refresh-456');

    expect(service.getAccessToken()).toBe('access-123');
    expect(service.getRefreshToken()).toBe('refresh-456');
    expect(globalThis.localStorage?.length ?? 0).toBe(0);

    service.clearTokens();
    expect(service.getAccessToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
  });
});
