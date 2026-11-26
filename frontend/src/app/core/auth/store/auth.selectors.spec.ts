import { describe, expect, it } from 'vitest';
import {
  selectActivationStatus,
  selectCurrentUser,
  selectCurrentUserRole,
  selectIsAuthenticated,
  selectIsAuthHydrated,
} from './auth.selectors';
import { AuthState } from './auth.state';

describe('authSelectors', () => {
  const state: AuthState = {
    user: {
      id: 'u-1',
      email: 'designer@h2l.local',
      externalId: null,
      systemRole: 'ProcessDesigner',
      preferredLanguage: 'de',
      preferredTheme: 'high-contrast',
      isActive: true,
      createdAt: '2026-11-25T20:00:00Z',
    },
    accessToken: 'at',
    refreshToken: 'rt',
    isAuthenticated: true,
    hydrated: true,
    loading: false,
    error: null,
    sessionExpiredNotice: false,
    forbiddenError: null,
    activationStatus: 'success',
    activationError: null,
    preferencesSaving: false,
    preferencesSaved: true,
    preferencesError: null,
  };

  it('projects user, role, authentication flags, and activation status', () => {
    expect(selectCurrentUser.projector(state)?.email).toBe('designer@h2l.local');
    expect(selectCurrentUserRole.projector(state.user)).toBe('ProcessDesigner');
    expect(selectIsAuthenticated.projector(state)).toBe(true);
    expect(selectIsAuthHydrated.projector(state)).toBe(true);
    expect(selectActivationStatus.projector(state)).toBe('success');
  });
});
