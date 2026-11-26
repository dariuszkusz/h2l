import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class ProjectsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  listProjects(): Observable<components['schemas']['ProjectResponse'][]> {
    return this.http.get<components['schemas']['ProjectResponse'][]>(`${this.baseUrl}/projects`);
  }

  createProject(
    payload: components['schemas']['CreateProjectRequest'],
  ): Observable<components['schemas']['ProjectResponse']> {
    return this.http.post<components['schemas']['ProjectResponse']>(
      `${this.baseUrl}/projects`,
      payload,
    );
  }

  listProjectProcesses(
    projectId: string,
  ): Observable<components['schemas']['WorkflowProcessResponse'][]> {
    return this.http.get<components['schemas']['WorkflowProcessResponse'][]>(
      `${this.baseUrl}/projects/${encodeURIComponent(projectId)}/processes`,
    );
  }

  createWorkflowProcess(
    projectId: string,
    payload: components['schemas']['CreateWorkflowProcessRequest'],
  ): Observable<components['schemas']['WorkflowProcessResponse']> {
    return this.http.post<components['schemas']['WorkflowProcessResponse']>(
      `${this.baseUrl}/projects/${encodeURIComponent(projectId)}/processes`,
      payload,
    );
  }

  listProcessForms(
    processId: string,
  ): Observable<components['schemas']['FormDefinitionResponse'][]> {
    return this.http.get<components['schemas']['FormDefinitionResponse'][]>(
      `${this.baseUrl}/processes/${encodeURIComponent(processId)}/forms`,
    );
  }

  createFormDefinition(
    processId: string,
    payload: components['schemas']['CreateFormDefinitionRequest'],
  ): Observable<components['schemas']['FormDefinitionResponse']> {
    return this.http.post<components['schemas']['FormDefinitionResponse']>(
      `${this.baseUrl}/processes/${encodeURIComponent(processId)}/forms`,
      payload,
    );
  }

  getFormAccessRules(formId: string): Observable<components['schemas']['FormAccessRulesResponse']> {
    return this.http.get<components['schemas']['FormAccessRulesResponse']>(
      `${this.baseUrl}/forms/${encodeURIComponent(formId)}/access-rules`,
    );
  }

  updateFormAccessRules(
    formId: string,
    payload: components['schemas']['UpdateFormAccessRulesRequest'],
  ): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/forms/${encodeURIComponent(formId)}/access-rules`,
      payload,
    );
  }
}
