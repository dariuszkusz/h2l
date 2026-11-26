import { LicensingActions } from './licensing.actions';
import { initialLicensingState, licensingReducer } from './licensing.reducer';

describe('licensingReducer', () => {
  const sampleStatus = {
    instanceId: '01900000-0000-7000-8000-000000000001',
    plan: 'Free' as const,
    status: 'Active' as const,
    inGracePeriod: false,
    usage: { activeProcesses: 1, activeConnectors: 0, adminUsers: 1, endUsers: 0 },
    limits: {
      maxActiveProcesses: 1,
      maxActiveConnectors: 1,
      maxAdminUsers: 1,
      maxEndUsers: null,
    },
  };

  it('loads status and handles success', () => {
    const loading = licensingReducer(initialLicensingState, LicensingActions.loadStatus());
    expect(loading.loading).toBe(true);

    const loaded = licensingReducer(
      loading,
      LicensingActions.loadStatusSuccess({ status: sampleStatus }),
    );
    expect(loaded.loading).toBe(false);
    expect(loaded.status?.plan).toBe('Free');
  });

  it('activates license and handles failure', () => {
    const activating = licensingReducer(
      initialLicensingState,
      LicensingActions.activateLicense({ request: { licenseKey: 'key' } }),
    );
    expect(activating.activating).toBe(true);

    const activated = licensingReducer(
      activating,
      LicensingActions.activateLicenseSuccess({ status: { ...sampleStatus, plan: 'Pro' } }),
    );
    expect(activated.activating).toBe(false);
    expect(activated.activationSuccess).toBe(true);
    expect(activated.status?.plan).toBe('Pro');

    const failed = licensingReducer(
      activating,
      LicensingActions.activateLicenseFailure({
        error: {
          status: 400,
          errorCode: 'LICENSE_INVALID',
          messageKey: 'errors.codes.LICENSE_INVALID',
          detail: 'Invalid key',
          fieldErrors: {},
          isOffline: false,
        },
      }),
    );
    expect(failed.error?.errorCode).toBe('LICENSE_INVALID');
    expect(failed.activationSuccess).toBe(false);
  });
});
