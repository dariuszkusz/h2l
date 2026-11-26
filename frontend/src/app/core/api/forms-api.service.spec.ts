import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { FormsApiService } from './forms-api.service';

describe('FormsApiService', () => {
  let service: FormsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FormsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('calls GET /forms/:id, PUT /forms/:id/draft, POST /forms/:id/publish, and GET /forms/:id/versions', () => {
    service.getFormDefinition('f1').subscribe();
    const getReq = httpMock.expectOne('/api/v1/forms/f1');
    expect(getReq.request.method).toBe('GET');
    getReq.flush({});

    service
      .updateFormDraft('f1', {
        name: 'Purchase Form',
        draftSchema: {
          schemaVersion: '1.0',
          defaultLanguage: 'en',
          layoutMode: 'single',
          steps: [{ id: 's1', title: { en: 'Step 1' }, components: [] }],
        },
      })
      .subscribe();
    const putReq = httpMock.expectOne('/api/v1/forms/f1/draft');
    expect(putReq.request.method).toBe('PUT');
    putReq.flush({});

    service.publishFormVersion('f1').subscribe();
    const pubReq = httpMock.expectOne('/api/v1/forms/f1/publish');
    expect(pubReq.request.method).toBe('POST');
    pubReq.flush({});

    service.listFormVersions('f1').subscribe();
    const verReq = httpMock.expectOne('/api/v1/forms/f1/versions');
    expect(verReq.request.method).toBe('GET');
    verReq.flush([]);
  });
});
