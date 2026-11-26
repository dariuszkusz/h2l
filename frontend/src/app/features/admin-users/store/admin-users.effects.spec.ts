import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { GroupsApiService } from '../../../core/api/groups-api.service';
import { UsersApiService } from '../../../core/api/users-api.service';
import { AdminUsersActions } from './admin-users.actions';
import { AdminUsersEffects } from './admin-users.effects';

describe('AdminUsersEffects', () => {
  let actions$: Observable<Action>;
  let effects: AdminUsersEffects;
  const usersApiMock = { listUsers: vi.fn(), createUser: vi.fn() };
  const groupsApiMock = {
    listGroups: vi.fn(),
    createGroup: vi.fn(),
    listGroupMembers: vi.fn(),
    addGroupMember: vi.fn(),
    removeGroupMember: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        AdminUsersEffects,
        provideMockActions(() => actions$),
        { provide: UsersApiService, useValue: usersApiMock },
        { provide: GroupsApiService, useValue: groupsApiMock },
      ],
    });
    effects = TestBed.inject(AdminUsersEffects);
  });

  it('loads users and maps 402 plan limit on invite failure', async () => {
    usersApiMock.listUsers.mockReturnValue(of([]));
    actions$ = of(AdminUsersActions.loadUsers());
    expect(await firstValueFrom(effects.loadUsers$)).toEqual(
      AdminUsersActions.loadUsersSuccess({ users: [] }),
    );

    usersApiMock.createUser.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 402,
            error: { errorCode: 'FREE_PLAN_ADMIN_LIMIT_EXCEEDED' },
          }),
      ),
    );
    actions$ = of(
      AdminUsersActions.inviteUser({
        request: { email: 'admin2@example.com', systemRole: 'Admin' },
      }),
    );
    const result = await firstValueFrom(effects.inviteUser$);
    expect(result.type).toBe(AdminUsersActions.inviteUserFailure.type);
  });

  it('creates groups and removes members', async () => {
    const group = {
      id: 'g1',
      name: 'Sales',
      code: 'SALES',
      memberCount: 0,
      createdAt: '2026-11-25T20:00:00Z',
    };
    groupsApiMock.createGroup.mockReturnValue(of(group));
    actions$ = of(AdminUsersActions.createGroup({ request: { name: 'Sales', code: 'SALES' } }));
    expect(await firstValueFrom(effects.createGroup$)).toEqual(
      AdminUsersActions.createGroupSuccess({ group }),
    );

    groupsApiMock.removeGroupMember.mockReturnValue(of(void 0));
    actions$ = of(AdminUsersActions.removeGroupMember({ groupId: 'g1', userId: 'u1' }));
    expect(await firstValueFrom(effects.removeGroupMember$)).toEqual(
      AdminUsersActions.removeGroupMemberSuccess({ groupId: 'g1', userId: 'u1' }),
    );
  });
});
