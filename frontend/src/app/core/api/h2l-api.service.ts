import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class H2lApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getSetupStatus(): Observable<components['schemas']['SetupStatusResponse']> {
    return this.http.get<components['schemas']['SetupStatusResponse']>(
      `${this.baseUrl}/setup/status`,
    );
  }

  updateUserPreferences(
    payload: components['schemas']['UpdateUserPreferencesRequest'],
  ): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/users/me/preferences`, payload);
  }
}
