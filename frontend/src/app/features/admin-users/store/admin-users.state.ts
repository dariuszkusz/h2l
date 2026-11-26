import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export interface AdminUsersState {
  readonly users: readonly components['schemas']['UserSummaryResponse'][];
  readonly groups: readonly components['schemas']['GroupResponse'][];
  readonly selectedGroupId: string | null;
  readonly groupMembers: readonly components['schemas']['GroupMemberResponse'][];
  readonly loadingUsers: boolean;
  readonly loadingGroups: boolean;
  readonly loadingMembers: boolean;
  readonly inviting: boolean;
  readonly creatingGroup: boolean;
  readonly updatingMembers: boolean;
  readonly lastInvitedUser: components['schemas']['CreateUserResponse'] | null;
  readonly error: MappedApiError | null;
  readonly successMessageKey: string | null;
}
