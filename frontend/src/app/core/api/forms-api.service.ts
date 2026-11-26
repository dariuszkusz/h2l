import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class FormsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getFormDefinition(
    formId: string,
  ): Observable<components['schemas']['FormDefinitionDetailResponse']> {
    return this.http.get<components['schemas']['FormDefinitionDetailResponse']>(
      `${this.baseUrl}/forms/${encodeURIComponent(formId)}`,
    );
  }

  updateFormDraft(
    formId: string,
    payload: components['schemas']['UpdateFormDraftRequest'],
  ): Observable<components['schemas']['FormDefinitionDetailResponse']> {
    return this.http.put<components['schemas']['FormDefinitionDetailResponse']>(
      `${this.baseUrl}/forms/${encodeURIComponent(formId)}/draft`,
      payload,
    );
  }

  publishFormVersion(
    formId: string,
  ): Observable<components['schemas']['FormVersionResponse']> {
    return this.http.post<components['schemas']['FormVersionResponse']>(
      `${this.baseUrl}/forms/${encodeURIComponent(formId)}/publish`,
      {},
    );
  }

  listFormVersions(
    formId: string,
  ): Observable<components['schemas']['FormVersionSummaryResponse'][]> {
    return this.http.get<components['schemas']['FormVersionSummaryResponse'][]>(
      `${this.baseUrl}/forms/${encodeURIComponent(formId)}/versions`,
    );
  }
}
