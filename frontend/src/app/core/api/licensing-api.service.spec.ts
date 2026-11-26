import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LicensingApiService } from './licensing-api.service';

describe('LicensingApiService', () => {
  let service: LicensingApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LicensingApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('fetches licensing status and activates a license key', () => {
    const freeStatus = {
      instanceId: '01900000-0000-7000-8000-000000000001',
      plan: 'Free' as const,
      status: 'Active' as const,
      inGracePeriod: false,
      usage: { activeProcesses: 1, activeConnectors: 0, adminUsers: 1, endUsers: 2 },
      limits: {
        maxActiveProcesses: 1,
        maxActiveConnectors: 1,
        maxAdminUsers: 1,
        maxEndUsers: null,
      },
    };

    service.getStatus().subscribe((status) => {
      expect(status.plan).toBe('Free');
    });
    httpMock.expectOne('/api/v1/licensing/status').flush(freeStatus);

    service.activate({ licenseKey: 'ed25519-pro-key' }).subscribe((status) => {
      expect(status.plan).toBe('Pro');
    });
    const postReq = httpMock.expectOne('/api/v1/licensing/activate');
    expect(postReq.request.method).toBe('POST');
    postReq.flush({ ...freeStatus, plan: 'Pro' });
  });
});
