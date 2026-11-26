import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AdminUsersState } from './admin-users.state';

export const selectAdminUsersState = createFeatureSelector<AdminUsersState>('adminUsers');

export const selectAdminUsersList = createSelector(
  selectAdminUsersState,
  (state) => state.users,
);

export const selectAdminGroupsList = createSelector(
  selectAdminUsersState,
  (state) => state.groups,
);

export const selectSelectedGroupId = createSelector(
  selectAdminUsersState,
  (state) => state.selectedGroupId,
);

export const selectSelectedGroup = createSelector(
  selectAdminGroupsList,
  selectSelectedGroupId,
  (groups, selectedId) => groups.find((g) => g.id === selectedId) ?? null,
);

export const selectGroupMembers = createSelector(
  selectAdminUsersState,
  (state) => state.groupMembers,
);

export const selectAdminUsersLoading = createSelector(
  selectAdminUsersState,
  (state) => state.loadingUsers || state.loadingGroups,
);

export const selectGroupMembersLoading = createSelector(
  selectAdminUsersState,
  (state) => state.loadingMembers,
);

export const selectAdminUsersSaving = createSelector(
  selectAdminUsersState,
  (state) => state.inviting || state.creatingGroup || state.updatingMembers,
);

export const selectLastInvitedUser = createSelector(
  selectAdminUsersState,
  (state) => state.lastInvitedUser,
);

export const selectAdminUsersError = createSelector(
  selectAdminUsersState,
  (state) => state.error,
);

export const selectAdminUsersSuccessKey = createSelector(
  selectAdminUsersState,
  (state) => state.successMessageKey,
);
