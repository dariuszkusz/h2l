import { createFeatureSelector, createSelector } from '@ngrx/store';
import { LicensingState } from './licensing.state';

export const selectLicensingState = createFeatureSelector<LicensingState>('licensing');

export const selectLicensingStatus = createSelector(
  selectLicensingState,
  (state) => state.status,
);

export const selectLicensingLoading = createSelector(
  selectLicensingState,
  (state) => state.loading,
);

export const selectLicensingActivating = createSelector(
  selectLicensingState,
  (state) => state.activating,
);

export const selectLicensingError = createSelector(
  selectLicensingState,
  (state) => state.error,
);

export const selectLicensingActivationSuccess = createSelector(
  selectLicensingState,
  (state) => state.activationSuccess,
);

export const selectIsInGracePeriod = createSelector(
  selectLicensingStatus,
  (status) => status?.inGracePeriod === true || status?.status === 'GracePeriod',
);

export const selectIsLicenseExpired = createSelector(
  selectLicensingStatus,
  (status) => status?.status === 'Expired',
);
