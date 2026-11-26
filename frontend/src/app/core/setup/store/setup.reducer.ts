import { createReducer, on } from '@ngrx/store';
import { SetupActions } from './setup.actions';
import { SetupState } from './setup.state';

export const initialSetupState: SetupState = {
  isInitialized: null,
  instanceId: null,
  loadingStatus: false,
  submitting: false,
  error: null,
  initializedSuccess: false,
  alreadyInitializedRedirect: false,
};

export const setupReducer = createReducer(
  initialSetupState,
  on(SetupActions.checkStatus, (state): SetupState => ({
    ...state,
    loadingStatus: true,
    error: null,
  })),
  on(SetupActions.checkStatusSuccess, (state, { status }): SetupState => ({
    ...state,
    isInitialized: status.isInitialized,
    instanceId: status.instanceId,
    loadingStatus: false,
    error: null,
  })),
  on(SetupActions.checkStatusFailure, (state, { error }): SetupState => ({
    ...state,
    loadingStatus: false,
    error,
  })),
  on(SetupActions.initialize, (state): SetupState => ({
    ...state,
    submitting: true,
    error: null,
    initializedSuccess: false,
  })),
  on(SetupActions.initializeSuccess, (state, { response }): SetupState => ({
    ...state,
    isInitialized: true,
    instanceId: response.instanceId,
    submitting: false,
    error: null,
    initializedSuccess: true,
  })),
  on(SetupActions.initializeAlreadyDone, (state, { error }): SetupState => ({
    ...state,
    isInitialized: true,
    submitting: false,
    error,
    alreadyInitializedRedirect: true,
  })),
  on(SetupActions.initializeFailure, (state, { error }): SetupState => ({
    ...state,
    submitting: false,
    error,
    initializedSuccess: false,
  })),
  on(SetupActions.clearSetupError, (state): SetupState => ({
    ...state,
    error: null,
  })),
  on(SetupActions.dismissAlreadyInitializedNotice, (state): SetupState => ({
    ...state,
    alreadyInitializedRedirect: false,
  })),
);
