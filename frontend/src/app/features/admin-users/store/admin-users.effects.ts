import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, switchMap } from 'rxjs';
import { GroupsApiService } from '../../../core/api/groups-api.service';
import { ProblemDetailsMapperService } from '../../../core/api/services/problem-details-mapper.service';
import { UsersApiService } from '../../../core/api/users-api.service';
import { AdminUsersActions } from './admin-users.actions';

@Injectable()
export class AdminUsersEffects {
  private readonly actions$ = inject(Actions);
  private readonly usersApi = inject(UsersApiService);
  private readonly groupsApi = inject(GroupsApiService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);

  readonly loadUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminUsersActions.loadUsers),
      switchMap(() =>
        this.usersApi.listUsers().pipe(
          map((users) => AdminUsersActions.loadUsersSuccess({ users })),
          catchError((error: unknown) =>
            of(AdminUsersActions.loadUsersFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        ),
      ),
    ),
  );

  readonly inviteUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminUsersActions.inviteUser),
      exhaustMap(({ request }) =>
        this.usersApi.createUser(request).pipe(
          map((user) => AdminUsersActions.inviteUserSuccess({ user })),
          catchError((error: unknown) =>
            of(
              AdminUsersActions.inviteUserFailure({ error: this.problemDetailsMapper.map(error) }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly loadGroups$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminUsersActions.loadGroups),
      switchMap(() =>
        this.groupsApi.listGroups().pipe(
          map((groups) => AdminUsersActions.loadGroupsSuccess({ groups })),
          catchError((error: unknown) =>
            of(
              AdminUsersActions.loadGroupsFailure({ error: this.problemDetailsMapper.map(error) }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly createGroup$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminUsersActions.createGroup),
      exhaustMap(({ request }) =>
        this.groupsApi.createGroup(request).pipe(
          map((group) => AdminUsersActions.createGroupSuccess({ group })),
          catchError((error: unknown) =>
            of(
              AdminUsersActions.createGroupFailure({ error: this.problemDetailsMapper.map(error) }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly loadGroupMembers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminUsersActions.loadGroupMembers),
      switchMap(({ groupId }) =>
        this.groupsApi.listGroupMembers(groupId).pipe(
          map((members) => AdminUsersActions.loadGroupMembersSuccess({ groupId, members })),
          catchError((error: unknown) =>
            of(
              AdminUsersActions.loadGroupMembersFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly addGroupMember$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminUsersActions.addGroupMember),
      exhaustMap(({ groupId, request }) =>
        this.groupsApi.addGroupMember(groupId, request).pipe(
          map((member) => AdminUsersActions.addGroupMemberSuccess({ groupId, member })),
          catchError((error: unknown) =>
            of(
              AdminUsersActions.addGroupMemberFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly removeGroupMember$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminUsersActions.removeGroupMember),
      exhaustMap(({ groupId, userId }) =>
        this.groupsApi.removeGroupMember(groupId, userId).pipe(
          map(() => AdminUsersActions.removeGroupMemberSuccess({ groupId, userId })),
          catchError((error: unknown) =>
            of(
              AdminUsersActions.removeGroupMemberFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );
}
