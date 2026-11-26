import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class TasksApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  listTasks(options?: {
    readonly scope?: components['schemas']['TaskListScope'];
    readonly status?: components['schemas']['HumanTaskStatus'];
    readonly page?: number;
    readonly pageSize?: number;
  }): Observable<components['schemas']['TaskListResponse']> {
    let params = new HttpParams();
    if (options?.scope) {
      params = params.set('scope', options.scope);
    }
    if (options?.status) {
      params = params.set('status', options.status);
    }
    if (options?.page !== undefined) {
      params = params.set('page', String(options.page));
    }
    if (options?.pageSize !== undefined) {
      params = params.set('pageSize', String(options.pageSize));
    }

    return this.http.get<components['schemas']['TaskListResponse']>(`${this.baseUrl}/tasks`, {
      params,
    });
  }

  getTaskDetail(taskId: string): Observable<components['schemas']['TaskDetailResponse']> {
    return this.http.get<components['schemas']['TaskDetailResponse']>(
      `${this.baseUrl}/tasks/${encodeURIComponent(taskId)}`,
    );
  }

  claimTask(taskId: string): Observable<components['schemas']['TaskDetailResponse']> {
    return this.http.post<components['schemas']['TaskDetailResponse']>(
      `${this.baseUrl}/tasks/${encodeURIComponent(taskId)}/claim`,
      {},
    );
  }

  unclaimTask(taskId: string): Observable<components['schemas']['TaskDetailResponse']> {
    return this.http.post<components['schemas']['TaskDetailResponse']>(
      `${this.baseUrl}/tasks/${encodeURIComponent(taskId)}/unclaim`,
      {},
    );
  }

  assignTask(
    taskId: string,
    payload: components['schemas']['AssignTaskRequest'],
  ): Observable<components['schemas']['TaskDetailResponse']> {
    return this.http.post<components['schemas']['TaskDetailResponse']>(
      `${this.baseUrl}/tasks/${encodeURIComponent(taskId)}/assign`,
      payload,
    );
  }

  submitTask(
    taskId: string,
    payload: components['schemas']['SubmitTaskRequest'],
    idempotencyKey: string,
  ): Observable<components['schemas']['SubmitTaskResponse']> {
    const headers = new HttpHeaders({
      'Idempotency-Key': idempotencyKey,
    });
    return this.http.post<components['schemas']['SubmitTaskResponse']>(
      `${this.baseUrl}/tasks/${encodeURIComponent(taskId)}/submit`,
      payload,
      { headers },
    );
  }

  uploadTaskAttachment(
    taskId: string,
    fieldVariableKey: string,
    file: File,
  ): Observable<components['schemas']['AttachmentUploadResponse']> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    formData.append('fieldVariableKey', fieldVariableKey);

    return this.http.post<components['schemas']['AttachmentUploadResponse']>(
      `${this.baseUrl}/tasks/${encodeURIComponent(taskId)}/attachments`,
      formData,
    );
  }
}
