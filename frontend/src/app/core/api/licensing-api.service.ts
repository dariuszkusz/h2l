import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class LicensingApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getStatus(): Observable<components['schemas']['LicensingStatusResponse']> {
    return this.http.get<components['schemas']['LicensingStatusResponse']>(
      `${this.baseUrl}/licensing/status`,
    );
  }

  activate(
    payload: components['schemas']['ActivateLicenseRequest'],
  ): Observable<components['schemas']['LicensingStatusResponse']> {
    return this.http.post<components['schemas']['LicensingStatusResponse']>(
      `${this.baseUrl}/licensing/activate`,
      payload,
    );
  }
}
