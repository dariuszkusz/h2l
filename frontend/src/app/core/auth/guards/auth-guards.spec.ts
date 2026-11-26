import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { provideStore, Store } from '@ngrx/store';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthActions } from '../store/auth.actions';
import { authReducer } from '../store/auth.reducer';
import { authGuard } from './auth.guard';
import { guestOnlyGuard } from './guest-only.guard';
import { roleGuard } from './role.guard';

const MOCK_ROUTE = {} as ActivatedRouteSnapshot;
const MOCK_STATE = { url: '/tasks' } as RouterStateSnapshot;

describe('Auth Route Guards', () => {
  let store: Store;

  beforeEach(() => {
    globalThis.sessionStorage?.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideStore({ auth: authReducer })],
    });
    store = TestBed.inject(Store);
  });

  it('authGuard redirects unauthenticated users to /login with returnUrl', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(MOCK_ROUTE, MOCK_STATE));
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/login?returnUrl=%2Ftasks');
  });

  it('authGuard allows authenticated users and guestOnlyGuard redirects them to /tasks', () => {
    store.dispatch(
      AuthActions.loginSuccess({
        response: {
          accessToken: 'at',
          refreshToken: 'rt',
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
        },
      }),
    );

    const authResult = TestBed.runInInjectionContext(() => authGuard(MOCK_ROUTE, MOCK_STATE));
    expect(authResult).toBe(true);

    const guestResult = TestBed.runInInjectionContext(() =>
      guestOnlyGuard(MOCK_ROUTE, MOCK_STATE),
    );
    expect(guestResult instanceof UrlTree).toBe(true);
    expect((guestResult as UrlTree).toString()).toBe('/tasks');
  });

  it('roleGuard allows permitted role and redirects unauthorized role to /tasks', () => {
    store.dispatch(
      AuthActions.loginSuccess({
        response: {
          accessToken: 'at',
          refreshToken: 'rt',
          expiresInSeconds: 900,
          user: {
            id: 'u-2',
            email: 'user@h2l.local',
            externalId: null,
            systemRole: 'EndUser',
            preferredLanguage: 'en',
            preferredTheme: 'light',
            isActive: true,
            createdAt: '2026-11-25T20:00:00Z',
          },
        },
      }),
    );

    const endUserGuard = roleGuard(['EndUser', 'Admin']);
    expect(
      TestBed.runInInjectionContext(() => endUserGuard(MOCK_ROUTE, MOCK_STATE)),
    ).toBe(true);

    const adminOnlyGuard = roleGuard(['Admin']);
    const deniedResult = TestBed.runInInjectionContext(() =>
      adminOnlyGuard(MOCK_ROUTE, MOCK_STATE),
    );
    expect(deniedResult instanceof UrlTree).toBe(true);
    expect((deniedResult as UrlTree).toString()).toBe('/tasks');
  });
});
