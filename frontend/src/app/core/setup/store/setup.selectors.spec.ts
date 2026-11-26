import { describe, expect, it } from 'vitest';
import {
  selectAlreadyInitializedRedirect,
  selectInstanceId,
  selectIsSetupInitialized,
  selectSetupInitializedSuccess,
  selectSetupLoadingStatus,
  selectSetupSubmitting,
} from './setup.selectors';
import { SetupState } from './setup.state';

describe('setupSelectors', () => {
  const state: SetupState = {
    isInitialized: true,
    instanceId: 'inst-1',
    loadingStatus: false,
    submitting: false,
    error: null,
    initializedSuccess: true,
    alreadyInitializedRedirect: false,
  };

  it('projects setup flags and instanceId', () => {
    expect(selectIsSetupInitialized.projector(state)).toBe(true);
    expect(selectInstanceId.projector(state)).toBe('inst-1');
    expect(selectSetupLoadingStatus.projector(state)).toBe(false);
    expect(selectSetupSubmitting.projector(state)).toBe(false);
    expect(selectSetupInitializedSuccess.projector(state)).toBe(true);
    expect(selectAlreadyInitializedRedirect.projector(state)).toBe(false);
  });
});
