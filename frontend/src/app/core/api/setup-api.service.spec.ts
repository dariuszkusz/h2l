import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { SetupApiService } from './setup-api.service';

describe('SetupApiService', () => {
  let service: SetupApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SetupApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests GET /api/v1/setup/status', () => {
    let result: { isInitialized: boolean; instanceId: string } | undefined;
    service.getStatus().subscribe((res) => {
      result = res;
    });

    const req = httpMock.expectOne('/api/v1/setup/status');
    expect(req.request.method).toBe('GET');
    req.flush({ isInitialized: false, instanceId: 'inst-1' });

    expect(result).toEqual({ isInitialized: false, instanceId: 'inst-1' });
  });

  it('posts POST /api/v1/setup/initialize with smtp null when disabled', () => {
    let result: { instanceId: string; adminUserId: string } | undefined;
    service
      .initialize({
        adminEmail: 'admin@h2l.local',
        adminPassword: 'VerySecurePassword123',
        defaultLanguage: 'en',
        selectedPalette: 'slate',
        smtp: null,
      })
      .subscribe((res) => {
        result = res;
      });

    const req = httpMock.expectOne('/api/v1/setup/initialize');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.smtp).toBeNull();
    req.flush({ instanceId: 'inst-1', adminUserId: 'user-1' });

    expect(result).toEqual({ instanceId: 'inst-1', adminUserId: 'user-1' });
  });
});
