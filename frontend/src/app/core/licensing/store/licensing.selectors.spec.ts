import {
  selectIsInGracePeriod,
  selectIsLicenseExpired,
  selectLicensingStatus,
} from './licensing.selectors';
import { LicensingState } from './licensing.state';

describe('licensing selectors', () => {
  it('selects grace period and expired flags', () => {
    const graceState: LicensingState = {
      status: {
        instanceId: 'inst-1',
        plan: 'Pro',
        status: 'GracePeriod',
        inGracePeriod: true,
        gracePeriodEndsAt: '2026-12-09T00:00:00Z',
        usage: { activeProcesses: 2, activeConnectors: 1, adminUsers: 1, endUsers: 5 },
        limits: {
          maxActiveProcesses: 10,
          maxActiveConnectors: 5,
          maxAdminUsers: 5,
          maxEndUsers: null,
        },
      },
      loading: false,
      activating: false,
      error: null,
      activationSuccess: false,
    };

    expect(selectLicensingStatus.projector(graceState)?.plan).toBe('Pro');
    expect(selectIsInGracePeriod.projector(graceState.status)).toBe(true);
    expect(selectIsLicenseExpired.projector(graceState.status)).toBe(false);

    const expiredStatus = { ...graceState.status!, status: 'Expired' as const, inGracePeriod: false };
    expect(selectIsInGracePeriod.projector(expiredStatus)).toBe(false);
    expect(selectIsLicenseExpired.projector(expiredStatus)).toBe(true);
  });
});
