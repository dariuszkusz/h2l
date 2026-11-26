import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Actions } from '@ngrx/effects';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, ReplaySubject, take, toArray } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OfflineDraftStorageService } from '../../connectivity/services/offline-draft-storage.service';
import { I18nActions } from '../../i18n/store/i18n.actions';
import { ThemeActions } from '../../theme/store/theme.actions';
import { AuthTokenStorageService } from '../services/auth-token-storage.service';
import { AuthActions } from './auth.actions';
import { AuthEffects } from './auth.effects';

const MOCK_USER = {
  id: 'u-1',
  email: 'admin@h2l.local',
  externalId: null,
  systemRole: 'Admin' as const,
  preferredLanguage: 'pl' as const,
  preferredTheme: 'dark' as const,
  isActive: true,
  createdAt: '2026-11-25T20:00:00Z',
};

describe('AuthEffects', () => {
  let actions$: ReplaySubject<Action>;
  let effects: AuthEffects;
  let httpMock: HttpTestingController;
  let tokenStorage: AuthTokenStorageService;
  let draftStorage: OfflineDraftStorageService;
  let router: Router;

  beforeEach(() => {
    globalThis.sessionStorage?.clear();
    actions$ = new ReplaySubject<Action>(1);

    TestBed.configureTestingModule({
      providers: [
        AuthEffects,
        provideMockActions(() => actions$),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    effects = TestBed.inject(AuthEffects);
    httpMock = TestBed.inject(HttpTestingController);
    tokenStorage = TestBed.inject(AuthTokenStorageService);
    draftStorage = TestBed.inject(OfflineDraftStorageService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('logs in, persists tokens, and syncs preferredLanguage and preferredTheme', async () => {
    const loginPromise = firstValueFrom(effects.login$);
    actions$.next(
      AuthActions.login({
        request: { email: 'admin@h2l.local', password: 'VerySecurePassword123' },
        returnUrl: '/settings',
      }),
    );

    const req = httpMock.expectOne('/api/v1/auth/login');
    req.flush({
      accessToken: 'at-1',
      refreshToken: 'rt-1',
      expiresInSeconds: 900,
      user: MOCK_USER,
    });

    const loginResult = await loginPromise;
    expect(loginResult).toEqual(
      AuthActions.loginSuccess({
        response: {
          accessToken: 'at-1',
          refreshToken: 'rt-1',
          expiresInSeconds: 900,
          user: MOCK_USER,
        },
        returnUrl: '/settings',
      }),
    );
    expect(tokenStorage.getAccessToken()).toBe('at-1');

    const langPromise = firstValueFrom(effects.syncLanguageOnAuth$);
    const themePromise = firstValueFrom(effects.syncThemeOnAuth$);
    actions$.next(loginResult);
    expect(await langPromise).toEqual(I18nActions.changeLanguage({ language: 'pl' }));
    expect(await themePromise).toEqual(ThemeActions.setThemeMode({ mode: 'dark' }));
  });

  it('clears tokens and IndexedDB drafts on logout even if API fails', async () => {
    tokenStorage.setTokens('at-1', 'rt-1');
    const clearDraftsSpy = vi
      .spyOn(draftStorage, 'clearAllDrafts')
      .mockResolvedValue(undefined);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const logoutPromise = firstValueFrom(effects.logout$);
    actions$.next(AuthActions.logout());

    const req = httpMock.expectOne('/api/v1/auth/logout');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    const result = await logoutPromise;
    expect(result).toEqual(AuthActions.logoutCompleted());
    expect(tokenStorage.getAccessToken()).toBeNull();
    expect(clearDraftsSpy).toHaveBeenCalledTimes(1);
    expect(navSpy).toHaveBeenCalledWith(['/login']);
  });

  it('skips hydrateSession when no tokens exist', async () => {
    const resultPromise = firstValueFrom(effects.hydrateSession$);
    actions$.next(AuthActions.hydrateSession());
    expect(await resultPromise).toEqual(AuthActions.hydrateSessionSkipped());
  });

  it('clears tokens and drafts on sessionExpired', async () => {
    tokenStorage.setTokens('at-1', 'rt-1');
    const clearDraftsSpy = vi
      .spyOn(draftStorage, 'clearAllDrafts')
      .mockResolvedValue(undefined);
    const navSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const expiredPromise = firstValueFrom(effects.handleSessionExpired$.pipe(take(1), toArray()));
    actions$.next(AuthActions.sessionExpired());

    await expiredPromise;
    expect(tokenStorage.getAccessToken()).toBeNull();
    expect(clearDraftsSpy).toHaveBeenCalledTimes(1);
    expect(navSpy).toHaveBeenCalledWith(['/login']);
  });

  it('activates account via POST /auth/activate', async () => {
    const activatePromise = firstValueFrom(effects.activateAccount$);
    actions$.next(
      AuthActions.activateAccount({
        request: { token: 'tok-123', password: 'VerySecurePassword123' },
      }),
    );

    const req = httpMock.expectOne('/api/v1/auth/activate');
    expect(req.request.method).toBe('POST');
    req.flush(null);

    expect(await activatePromise).toEqual(AuthActions.activateAccountSuccess());
    expect(TestBed.inject(Actions)).toBeDefined();
  });
});
