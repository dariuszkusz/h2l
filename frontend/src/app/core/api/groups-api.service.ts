import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-base-url.token';
import { components } from './generated/openapi.types';

@Injectable({
  providedIn: 'root',
})
export class GroupsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  listGroups(): Observable<components['schemas']['GroupResponse'][]> {
    return this.http.get<components['schemas']['GroupResponse'][]>(`${this.baseUrl}/groups`);
  }

  createGroup(
    payload: components['schemas']['CreateGroupRequest'],
  ): Observable<components['schemas']['GroupResponse']> {
    return this.http.post<components['schemas']['GroupResponse']>(
      `${this.baseUrl}/groups`,
      payload,
    );
  }

  listGroupMembers(groupId: string): Observable<components['schemas']['GroupMemberResponse'][]> {
    return this.http.get<components['schemas']['GroupMemberResponse'][]>(
      `${this.baseUrl}/groups/${encodeURIComponent(groupId)}/members`,
    );
  }

  addGroupMember(
    groupId: string,
    payload: components['schemas']['AddGroupMemberRequest'],
  ): Observable<components['schemas']['GroupMemberResponse']> {
    return this.http.post<components['schemas']['GroupMemberResponse']>(
      `${this.baseUrl}/groups/${encodeURIComponent(groupId)}/members`,
      payload,
    );
  }

  removeGroupMember(groupId: string, userId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/groups/${encodeURIComponent(groupId)}/members/${encodeURIComponent(userId)}`,
    );
  }
}
