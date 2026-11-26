import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { UsersApiService } from './users-api.service';

describe('UsersApiService', () => {
  let service: UsersApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(UsersApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('calls /users/me, /users/me/preferences, and /users endpoints', () => {
    service.getMe().subscribe();
    const meReq = httpMock.expectOne('/api/v1/users/me');
    expect(meReq.request.method).toBe('GET');
    meReq.flush({});

    service.updateMyPreferences({ preferredLanguage: 'pl' }).subscribe();
    const prefReq = httpMock.expectOne('/api/v1/users/me/preferences');
    expect(prefReq.request.method).toBe('PATCH');
    prefReq.flush(null);

    service.listUsers().subscribe();
    const listReq = httpMock.expectOne('/api/v1/users');
    expect(listReq.request.method).toBe('GET');
    listReq.flush([]);

    service.createUser({ email: 'new@h2l.local', systemRole: 'EndUser' }).subscribe();
    const createReq = httpMock.expectOne('/api/v1/users');
    expect(createReq.request.method).toBe('POST');
    createReq.flush({});
  });
});
