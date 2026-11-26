import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ConnectorsApiService } from './connectors-api.service';

describe('ConnectorsApiService', () => {
  let service: ConnectorsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ConnectorsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('calls connector CRUD, connection test, and dead-letter endpoints', () => {
    service.listConnectors().subscribe();
    const listReq = httpMock.expectOne('/api/v1/connectors');
    expect(listReq.request.method).toBe('GET');
    listReq.flush({ items: [] });

    service
      .createConnector({
        name: 'Camunda Prod',
        connectorType: 'Camunda8',
        baseUrl: 'https://camunda.internal:8080',
        authType: 'OidcClientCredentials',
        isActive: true,
      })
      .subscribe();
    const createReq = httpMock.expectOne('/api/v1/connectors');
    expect(createReq.request.method).toBe('POST');
    expect(createReq.request.body.name).toBe('Camunda Prod');
    createReq.flush({});

    service
      .updateConnector('conn-1', {
        name: 'Camunda Updated',
        connectorType: 'Camunda8',
        baseUrl: 'https://camunda.internal:8080',
        authType: 'None',
        clearCustomCaCertificate: false,
        isActive: true,
      })
      .subscribe();
    const updateReq = httpMock.expectOne('/api/v1/connectors/conn-1');
    expect(updateReq.request.method).toBe('PUT');
    updateReq.flush({});

    service.testConnectorConnection('conn-1').subscribe();
    const testReq = httpMock.expectOne('/api/v1/connectors/conn-1/test');
    expect(testReq.request.method).toBe('POST');
    testReq.flush({ isReachable: true, latencyMs: 42 });

    service.deleteConnector('conn-1').subscribe();
    const deleteReq = httpMock.expectOne('/api/v1/connectors/conn-1');
    expect(deleteReq.request.method).toBe('DELETE');
    deleteReq.flush(null);

    service.listDeadLetters().subscribe();
    const dlListReq = httpMock.expectOne('/api/v1/outbox/dead-letters');
    expect(dlListReq.request.method).toBe('GET');
    dlListReq.flush({ items: [] });

    service.retryDeadLetter('outbox-1').subscribe();
    const dlRetryReq = httpMock.expectOne('/api/v1/outbox/dead-letters/outbox-1/retry');
    expect(dlRetryReq.request.method).toBe('POST');
    dlRetryReq.flush(null);
  });
});
