import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export const AdminUsersActions = createActionGroup({
  source: 'AdminUsers',
  events: {
    'Load Users': emptyProps(),
    'Load Users Success': props<{ users: components['schemas']['UserSummaryResponse'][] }>(),
    'Load Users Failure': props<{ error: MappedApiError }>(),

    'Invite User': props<{ request: components['schemas']['CreateUserRequest'] }>(),
    'Invite User Success': props<{ user: components['schemas']['CreateUserResponse'] }>(),
    'Invite User Failure': props<{ error: MappedApiError }>(),

    'Load Groups': emptyProps(),
    'Load Groups Success': props<{ groups: components['schemas']['GroupResponse'][] }>(),
    'Load Groups Failure': props<{ error: MappedApiError }>(),

    'Create Group': props<{ request: components['schemas']['CreateGroupRequest'] }>(),
    'Create Group Success': props<{ group: components['schemas']['GroupResponse'] }>(),
    'Create Group Failure': props<{ error: MappedApiError }>(),

    'Select Group': props<{ groupId: string | null }>(),
    'Load Group Members': props<{ groupId: string }>(),
    'Load Group Members Success': props<{
      groupId: string;
      members: components['schemas']['GroupMemberResponse'][];
    }>(),
    'Load Group Members Failure': props<{ error: MappedApiError }>(),

    'Add Group Member': props<{
      groupId: string;
      request: components['schemas']['AddGroupMemberRequest'];
    }>(),
    'Add Group Member Success': props<{
      groupId: string;
      member: components['schemas']['GroupMemberResponse'];
    }>(),
    'Add Group Member Failure': props<{ error: MappedApiError }>(),

    'Remove Group Member': props<{ groupId: string; userId: string }>(),
    'Remove Group Member Success': props<{ groupId: string; userId: string }>(),
    'Remove Group Member Failure': props<{ error: MappedApiError }>(),

    'Clear Feedback': emptyProps(),
  },
});
