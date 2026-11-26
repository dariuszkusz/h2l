import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  login(
    payload: components['schemas']['LoginRequest'],
  ): Observable<components['schemas']['AuthTokenResponse']> {
    return this.http.post<components['schemas']['AuthTokenResponse']>(
      `${this.baseUrl}/auth/login`,
      payload,
    );
  }

  refresh(
    payload: components['schemas']['RefreshTokenRequest'],
  ): Observable<components['schemas']['AuthTokenResponse']> {
    return this.http.post<components['schemas']['AuthTokenResponse']>(
      `${this.baseUrl}/auth/refresh`,
      payload,
    );
  }

  logout(payload: components['schemas']['LogoutRequest']): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/auth/logout`, payload);
  }

  activate(payload: components['schemas']['ActivateAccountRequest']): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/auth/activate`, payload);
  }
}
