import { describe, expect, it } from 'vitest';
import { MappedApiError } from '../../api/models/mapped-api-error.model';
import { SetupActions } from './setup.actions';
import { initialSetupState, setupReducer } from './setup.reducer';

const CONFLICT_ERROR: MappedApiError = {
  status: 409,
  errorCode: 'SETUP_ALREADY_INITIALIZED',
  messageKey: 'errors.codes.SETUP_ALREADY_INITIALIZED',
  detail: null,
  fieldErrors: {},
  isOffline: false,
};

describe('setupReducer', () => {
  it('stores setup status from checkStatusSuccess', () => {
    const loading = setupReducer(initialSetupState, SetupActions.checkStatus());
    expect(loading.loadingStatus).toBe(true);

    const loaded = setupReducer(
      loading,
      SetupActions.checkStatusSuccess({
        status: { isInitialized: false, instanceId: 'inst-1' },
      }),
    );
    expect(loaded.loadingStatus).toBe(false);
    expect(loaded.isInitialized).toBe(false);
    expect(loaded.instanceId).toBe('inst-1');
  });

  it('marks initializedSuccess on initializeSuccess and alreadyInitializedRedirect on 409', () => {
    const submitting = setupReducer(
      initialSetupState,
      SetupActions.initialize({
        request: {
          adminEmail: 'admin@h2l.local',
          adminPassword: 'VerySecurePassword123',
          defaultLanguage: 'en',
          selectedPalette: 'slate',
          smtp: null,
        },
      }),
    );
    expect(submitting.submitting).toBe(true);

    const initialized = setupReducer(
      submitting,
      SetupActions.initializeSuccess({
        response: { instanceId: 'inst-1', adminUserId: 'admin-1' },
        language: 'pl',
        palette: 'ocean',
      }),
    );
    expect(initialized.isInitialized).toBe(true);
    expect(initialized.initializedSuccess).toBe(true);

    const alreadyDone = setupReducer(
      submitting,
      SetupActions.initializeAlreadyDone({ error: CONFLICT_ERROR }),
    );
    expect(alreadyDone.isInitialized).toBe(true);
    expect(alreadyDone.alreadyInitializedRedirect).toBe(true);
  });
});
