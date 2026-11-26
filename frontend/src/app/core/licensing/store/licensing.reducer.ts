import { createReducer, on } from '@ngrx/store';
import { LicensingActions } from './licensing.actions';
import { LicensingState } from './licensing.state';

export const initialLicensingState: LicensingState = {
  status: null,
  loading: false,
  activating: false,
  error: null,
  activationSuccess: false,
};

export const licensingReducer = createReducer(
  initialLicensingState,
  on(LicensingActions.loadStatus, (state): LicensingState => ({
    ...state,
    loading: true,
    error: null,
  })),
  on(LicensingActions.loadStatusSuccess, (state, { status }): LicensingState => ({
    ...state,
    status,
    loading: false,
    error: null,
  })),
  on(LicensingActions.loadStatusFailure, (state, { error }): LicensingState => ({
    ...state,
    loading: false,
    error,
  })),
  on(LicensingActions.activateLicense, (state): LicensingState => ({
    ...state,
    activating: true,
    error: null,
    activationSuccess: false,
  })),
  on(LicensingActions.activateLicenseSuccess, (state, { status }): LicensingState => ({
    ...state,
    status,
    activating: false,
    error: null,
    activationSuccess: true,
  })),
  on(LicensingActions.activateLicenseFailure, (state, { error }): LicensingState => ({
    ...state,
    activating: false,
    error,
    activationSuccess: false,
  })),
  on(LicensingActions.clearFeedback, (state): LicensingState => ({
    ...state,
    error: null,
    activationSuccess: false,
  })),
);
