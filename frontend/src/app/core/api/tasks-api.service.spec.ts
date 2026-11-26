import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { TasksApiService } from './tasks-api.service';

describe('TasksApiService', () => {
  let service: TasksApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TasksApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sends scope query param, claim/unclaim posts, and Idempotency-Key header on submit', () => {
    service.listTasks({ scope: 'group_queue' }).subscribe();
    const listReq = httpMock.expectOne('/api/v1/tasks?scope=group_queue');
    expect(listReq.request.method).toBe('GET');
    listReq.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });

    service.claimTask('t1').subscribe();
    const claimReq = httpMock.expectOne('/api/v1/tasks/t1/claim');
    expect(claimReq.request.method).toBe('POST');
    claimReq.flush({});

    service.unclaimTask('t1').subscribe();
    const unclaimReq = httpMock.expectOne('/api/v1/tasks/t1/unclaim');
    expect(unclaimReq.request.method).toBe('POST');
    unclaimReq.flush({});

    service.submitTask('t1', { values: { approved: true } }, 'idem-123').subscribe();
    const submitReq = httpMock.expectOne('/api/v1/tasks/t1/submit');
    expect(submitReq.request.method).toBe('POST');
    expect(submitReq.request.headers.get('Idempotency-Key')).toBe('idem-123');
    submitReq.flush({});

    service.assignTask('t1', { userId: 'u-2' }).subscribe();
    const assignReq = httpMock.expectOne('/api/v1/tasks/t1/assign');
    expect(assignReq.request.method).toBe('POST');
    expect(assignReq.request.body).toEqual({ userId: 'u-2' });
    assignReq.flush({});
  });
});
