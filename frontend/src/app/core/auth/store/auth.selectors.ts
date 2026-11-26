import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AuthState } from './auth.state';

export const selectAuthState = createFeatureSelector<AuthState>('auth');

export const selectCurrentUser = createSelector(selectAuthState, (state) => state.user);

export const selectCurrentUserRole = createSelector(
  selectCurrentUser,
  (user) => user?.systemRole ?? null,
);

export const selectIsAuthenticated = createSelector(
  selectAuthState,
  (state) => state.isAuthenticated,
);

export const selectIsAuthHydrated = createSelector(
  selectAuthState,
  (state) => state.hydrated,
);

export const selectAuthLoading = createSelector(selectAuthState, (state) => state.loading);

export const selectAuthError = createSelector(selectAuthState, (state) => state.error);

export const selectSessionExpiredNotice = createSelector(
  selectAuthState,
  (state) => state.sessionExpiredNotice,
);

export const selectForbiddenError = createSelector(
  selectAuthState,
  (state) => state.forbiddenError,
);

export const selectActivationStatus = createSelector(
  selectAuthState,
  (state) => state.activationStatus,
);

export const selectActivationError = createSelector(
  selectAuthState,
  (state) => state.activationError,
);

export const selectPreferencesSaving = createSelector(
  selectAuthState,
  (state) => state.preferencesSaving,
);

export const selectPreferencesSaved = createSelector(
  selectAuthState,
  (state) => state.preferencesSaved,
);

export const selectPreferencesError = createSelector(
  selectAuthState,
  (state) => state.preferencesError,
);
