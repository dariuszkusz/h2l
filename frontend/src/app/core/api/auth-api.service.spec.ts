import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AuthApiService } from './auth-api.service';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('calls login, refresh, logout, and activate endpoints', () => {
    service.login({ email: 'a@b.com', password: 'secret' }).subscribe();
    const loginReq = httpMock.expectOne('/api/v1/auth/login');
    expect(loginReq.request.method).toBe('POST');
    loginReq.flush({});

    service.refresh({ refreshToken: 'rt-1' }).subscribe();
    const refreshReq = httpMock.expectOne('/api/v1/auth/refresh');
    expect(refreshReq.request.method).toBe('POST');
    refreshReq.flush({});

    service.logout({ refreshToken: 'rt-1' }).subscribe();
    const logoutReq = httpMock.expectOne('/api/v1/auth/logout');
    expect(logoutReq.request.method).toBe('POST');
    logoutReq.flush(null);

    service.activate({ token: 'tok-1', password: 'LongPassword123' }).subscribe();
    const activateReq = httpMock.expectOne('/api/v1/auth/activate');
    expect(activateReq.request.method).toBe('POST');
    activateReq.flush(null);
  });
});
