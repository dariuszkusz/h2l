import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { LicensingApiService } from '../../api/licensing-api.service';
import { LicensingActions } from './licensing.actions';
import { LicensingEffects } from './licensing.effects';

describe('LicensingEffects', () => {
  let actions$: Observable<Action>;
  let effects: LicensingEffects;
  const licensingApiMock = {
    getStatus: vi.fn(),
    activate: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        LicensingEffects,
        provideMockActions(() => actions$),
        { provide: LicensingApiService, useValue: licensingApiMock },
      ],
    });
    effects = TestBed.inject(LicensingEffects);
  });

  it('dispatches loadStatusSuccess on loadStatus', async () => {
    const status = {
      instanceId: 'inst-1',
      plan: 'Free' as const,
      status: 'Active' as const,
      inGracePeriod: false,
      usage: { activeProcesses: 0, activeConnectors: 0, adminUsers: 1, endUsers: 0 },
      limits: {
        maxActiveProcesses: 1,
        maxActiveConnectors: 1,
        maxAdminUsers: 1,
        maxEndUsers: null,
      },
    };
    licensingApiMock.getStatus.mockReturnValue(of(status));
    actions$ = of(LicensingActions.loadStatus());

    const result = await firstValueFrom(effects.loadStatus$);
    expect(result).toEqual(LicensingActions.loadStatusSuccess({ status }));
  });

  it('maps ProblemDetails on activateLicenseFailure', async () => {
    licensingApiMock.activate.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { errorCode: 'INSTANCE_MISMATCH', detail: 'Wrong instance' },
          }),
      ),
    );
    actions$ = of(LicensingActions.activateLicense({ request: { licenseKey: 'bad' } }));

    const result = await firstValueFrom(effects.activateLicense$);
    expect(result.type).toBe(LicensingActions.activateLicenseFailure.type);
  });
});
