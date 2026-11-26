import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class UsersApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getMe(): Observable<components['schemas']['UserProfileResponse']> {
    return this.http.get<components['schemas']['UserProfileResponse']>(
      `${this.baseUrl}/users/me`,
    );
  }

  updateMyPreferences(
    payload: components['schemas']['UpdateUserPreferencesRequest'],
  ): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/users/me/preferences`, payload);
  }

  listUsers(): Observable<components['schemas']['UserSummaryResponse'][]> {
    return this.http.get<components['schemas']['UserSummaryResponse'][]>(
      `${this.baseUrl}/users`,
    );
  }

  createUser(
    payload: components['schemas']['CreateUserRequest'],
  ): Observable<components['schemas']['CreateUserResponse']> {
    return this.http.post<components['schemas']['CreateUserResponse']>(
      `${this.baseUrl}/users`,
      payload,
    );
  }
}
