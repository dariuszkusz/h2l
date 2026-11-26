import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Action, provideStore, Store } from '@ngrx/store';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthTokenStorageService } from '../services/auth-token-storage.service';
import { AuthActions } from '../store/auth.actions';
import { authReducer } from '../store/auth.reducer';
import { jwtInterceptor } from './jwt.interceptor';

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let tokenStorage: AuthTokenStorageService;
  let store: Store;
  let dispatchSpy: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    globalThis.sessionStorage?.clear();
    TestBed.configureTestingModule({
      providers: [
        provideStore({ auth: authReducer }),
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    tokenStorage = TestBed.inject(AuthTokenStorageService);
    store = TestBed.inject(Store);
    dispatchSpy = vi.spyOn(store, 'dispatch');
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('skips Authorization header on public endpoints and attaches Bearer on protected endpoints', () => {
    tokenStorage.setTokens('access-1', 'refresh-1');

    http.get('/api/v1/setup/status').subscribe();
    const publicReq = httpMock.expectOne('/api/v1/setup/status');
    expect(publicReq.request.headers.has('Authorization')).toBe(false);
    publicReq.flush({ isInitialized: true, instanceId: 'inst-1' });

    http.get('/api/v1/users/me').subscribe();
    const protectedReq = httpMock.expectOne('/api/v1/users/me');
    expect(protectedReq.request.headers.get('Authorization')).toBe('Bearer access-1');
    protectedReq.flush({});
  });

  it('performs single-flight refresh on concurrent 401 responses and retries queued requests', () => {
    tokenStorage.setTokens('expired-access', 'valid-refresh');

    const results: unknown[] = [];
    http.get('/api/v1/users/me').subscribe((res) => results.push(res));
    http.get('/api/v1/users').subscribe((res) => results.push(res));

    const firstMeReq = httpMock.expectOne('/api/v1/users/me');
    const firstUsersReq = httpMock.expectOne('/api/v1/users');

    firstMeReq.flush({ status: 401 }, { status: 401, statusText: 'Unauthorized' });
    firstUsersReq.flush({ status: 401 }, { status: 401, statusText: 'Unauthorized' });

    // Only a single POST /auth/refresh must be issued
    const refreshReq = httpMock.expectOne('/api/v1/auth/refresh');
    expect(refreshReq.request.method).toBe('POST');
    expect(refreshReq.request.body).toEqual({ refreshToken: 'valid-refresh' });

    refreshReq.flush({
      accessToken: 'fresh-access',
      refreshToken: 'fresh-refresh',
      expiresInSeconds: 900,
      user: {
        id: 'u-1',
        email: 'admin@h2l.local',
        externalId: null,
        systemRole: 'Admin',
        preferredLanguage: 'en',
        preferredTheme: 'light',
        isActive: true,
        createdAt: '2026-11-25T20:00:00Z',
      },
    });

    const retryMeReq = httpMock.expectOne('/api/v1/users/me');
    const retryUsersReq = httpMock.expectOne('/api/v1/users');
    expect(retryMeReq.request.headers.get('Authorization')).toBe('Bearer fresh-access');
    expect(retryUsersReq.request.headers.get('Authorization')).toBe('Bearer fresh-access');

    retryMeReq.flush({ id: 'u-1' });
    retryUsersReq.flush([]);

    expect(results).toHaveLength(2);
    expect(tokenStorage.getAccessToken()).toBe('fresh-access');
  });

  it('dispatches sessionExpired when refresh token is missing or refresh fails', () => {
    tokenStorage.setTokens('expired-access', 'bad-refresh');

    let errored = false;
    http.get('/api/v1/users/me').subscribe({
      error: () => {
        errored = true;
      },
    });

    const meReq = httpMock.expectOne('/api/v1/users/me');
    meReq.flush({}, { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne('/api/v1/auth/refresh');
    refreshReq.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(errored).toBe(true);
    const dispatchedTypes = dispatchSpy.mock.calls.map((call: [Action]) => call[0].type);
    expect(dispatchedTypes).toContain(AuthActions.sessionExpired.type);
  });

  it('dispatches forbiddenEncountered without refreshing on 403', () => {
    tokenStorage.setTokens('valid-access', 'valid-refresh');

    let errored = false;
    http.get('/api/v1/users').subscribe({
      error: () => {
        errored = true;
      },
    });

    const usersReq = httpMock.expectOne('/api/v1/users');
    usersReq.flush(
      {
        status: 403,
        errorCode: 'FORBIDDEN',
        detail: 'Admin role required.',
      },
      { status: 403, statusText: 'Forbidden' },
    );

    httpMock.expectNone('/api/v1/auth/refresh');
    expect(errored).toBe(true);

    const dispatchedTypes = dispatchSpy.mock.calls.map((call: [Action]) => call[0].type);
    expect(dispatchedTypes).toContain(AuthActions.forbiddenEncountered.type);
  });
});
