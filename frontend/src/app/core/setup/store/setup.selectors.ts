import { createFeatureSelector, createSelector } from '@ngrx/store';
import { SetupState } from './setup.state';

export const selectSetupState = createFeatureSelector<SetupState>('setup');

export const selectIsSetupInitialized = createSelector(
  selectSetupState,
  (state) => state.isInitialized,
);

export const selectInstanceId = createSelector(
  selectSetupState,
  (state) => state.instanceId,
);

export const selectSetupLoadingStatus = createSelector(
  selectSetupState,
  (state) => state.loadingStatus,
);

export const selectSetupSubmitting = createSelector(
  selectSetupState,
  (state) => state.submitting,
);

export const selectSetupError = createSelector(
  selectSetupState,
  (state) => state.error,
);

export const selectSetupInitializedSuccess = createSelector(
  selectSetupState,
  (state) => state.initializedSuccess,
);

export const selectAlreadyInitializedRedirect = createSelector(
  selectSetupState,
  (state) => state.alreadyInitializedRedirect,
);
