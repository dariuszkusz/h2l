import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class ConnectorsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  listConnectors(): Observable<components['schemas']['ConnectorListResponse']> {
    return this.http.get<components['schemas']['ConnectorListResponse']>(
      `${this.baseUrl}/connectors`,
    );
  }

  createConnector(
    payload: components['schemas']['CreateConnectorRequest'],
  ): Observable<components['schemas']['ConnectorSummaryResponse']> {
    return this.http.post<components['schemas']['ConnectorSummaryResponse']>(
      `${this.baseUrl}/connectors`,
      payload,
    );
  }

  updateConnector(
    connectorId: string,
    payload: components['schemas']['UpdateConnectorRequest'],
  ): Observable<components['schemas']['ConnectorSummaryResponse']> {
    return this.http.put<components['schemas']['ConnectorSummaryResponse']>(
      `${this.baseUrl}/connectors/${encodeURIComponent(connectorId)}`,
      payload,
    );
  }

  deleteConnector(connectorId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/connectors/${encodeURIComponent(connectorId)}`,
    );
  }

  testConnectorConnection(
    connectorId: string,
  ): Observable<components['schemas']['TestConnectorConnectionResponse']> {
    return this.http.post<components['schemas']['TestConnectorConnectionResponse']>(
      `${this.baseUrl}/connectors/${encodeURIComponent(connectorId)}/test`,
      {},
    );
  }

  listDeadLetters(): Observable<components['schemas']['DeadLetterListResponse']> {
    return this.http.get<components['schemas']['DeadLetterListResponse']>(
      `${this.baseUrl}/outbox/dead-letters`,
    );
  }

  retryDeadLetter(outboxId: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/outbox/dead-letters/${encodeURIComponent(outboxId)}/retry`,
      {},
    );
  }
}
