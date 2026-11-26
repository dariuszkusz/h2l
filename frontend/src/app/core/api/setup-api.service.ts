import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class SetupApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getStatus(): Observable<components['schemas']['SetupStatusResponse']> {
    return this.http.get<components['schemas']['SetupStatusResponse']>(
      `${this.baseUrl}/setup/status`,
    );
  }

  initialize(
    payload: components['schemas']['InitializeSetupRequest'],
  ): Observable<components['schemas']['InitializeSetupResponse']> {
    return this.http.post<components['schemas']['InitializeSetupResponse']>(
      `${this.baseUrl}/setup/initialize`,
      payload,
    );
  }
}
