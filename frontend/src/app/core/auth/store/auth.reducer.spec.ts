import { describe, expect, it } from 'vitest';
import { MappedApiError } from '../../api/models/mapped-api-error.model';
import { AuthActions } from './auth.actions';
import { authReducer, initialAuthState } from './auth.reducer';

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

const MOCK_ERROR: MappedApiError = {
  status: 401,
  errorCode: 'INVALID_CREDENTIALS',
  messageKey: 'errors.codes.INVALID_CREDENTIALS',
  detail: null,
  fieldErrors: {},
  isOffline: false,
};

describe('authReducer', () => {
  it('handles login and loginSuccess', () => {
    const loadingState = authReducer(
      initialAuthState,
      AuthActions.login({ request: { email: 'admin@h2l.local', password: 'secret' } }),
    );
    expect(loadingState.loading).toBe(true);

    const loggedInState = authReducer(
      loadingState,
      AuthActions.loginSuccess({
        response: {
          accessToken: 'at-1',
          refreshToken: 'rt-1',
          expiresInSeconds: 900,
          user: MOCK_USER,
        },
      }),
    );

    expect(loggedInState.isAuthenticated).toBe(true);
    expect(loggedInState.hydrated).toBe(true);
    expect(loggedInState.user).toEqual(MOCK_USER);
    expect(loggedInState.loading).toBe(false);
  });

  it('clears auth state and sets sessionExpiredNotice on sessionExpired', () => {
    const loggedIn = authReducer(
      initialAuthState,
      AuthActions.loginSuccess({
        response: {
          accessToken: 'at-1',
          refreshToken: 'rt-1',
          expiresInSeconds: 900,
          user: MOCK_USER,
        },
      }),
    );

    const expired = authReducer(loggedIn, AuthActions.sessionExpired());
    expect(expired.isAuthenticated).toBe(false);
    expect(expired.user).toBeNull();
    expect(expired.accessToken).toBeNull();
    expect(expired.sessionExpiredNotice).toBe(true);
  });

  it('tracks account activation lifecycle', () => {
    const loading = authReducer(
      initialAuthState,
      AuthActions.activateAccount({ request: { token: 't', password: 'VeryLongPassword123' } }),
    );
    expect(loading.activationStatus).toBe('loading');

    const failed = authReducer(loading, AuthActions.activateAccountFailure({ error: MOCK_ERROR }));
    expect(failed.activationStatus).toBe('error');
    expect(failed.activationError).toEqual(MOCK_ERROR);

    const succeeded = authReducer(loading, AuthActions.activateAccountSuccess());
    expect(succeeded.activationStatus).toBe('success');
  });
});
