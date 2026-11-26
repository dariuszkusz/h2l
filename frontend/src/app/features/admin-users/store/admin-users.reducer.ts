import { createReducer, on } from '@ngrx/store';
import { AdminUsersActions } from './admin-users.actions';
import { AdminUsersState } from './admin-users.state';

export const initialAdminUsersState: AdminUsersState = {
  users: [],
  groups: [],
  selectedGroupId: null,
  groupMembers: [],
  loadingUsers: false,
  loadingGroups: false,
  loadingMembers: false,
  inviting: false,
  creatingGroup: false,
  updatingMembers: false,
  lastInvitedUser: null,
  error: null,
  successMessageKey: null,
};

export const adminUsersReducer = createReducer(
  initialAdminUsersState,
  on(AdminUsersActions.loadUsers, (state): AdminUsersState => ({
    ...state,
    loadingUsers: true,
    error: null,
  })),
  on(AdminUsersActions.loadUsersSuccess, (state, { users }): AdminUsersState => ({
    ...state,
    users,
    loadingUsers: false,
  })),
  on(AdminUsersActions.loadUsersFailure, (state, { error }): AdminUsersState => ({
    ...state,
    loadingUsers: false,
    error,
  })),
  on(AdminUsersActions.inviteUser, (state): AdminUsersState => ({
    ...state,
    inviting: true,
    error: null,
    successMessageKey: null,
  })),
  on(AdminUsersActions.inviteUserSuccess, (state, { user }): AdminUsersState => ({
    ...state,
    inviting: false,
    lastInvitedUser: user,
    successMessageKey:
      user.deliveryMethod === 'smtp'
        ? 'adminUsers.invite.successSmtp'
        : 'adminUsers.invite.successManualLink',
    users: [
      ...state.users,
      {
        id: user.id,
        email: user.email,
        externalId: user.externalId ?? null,
        systemRole: user.systemRole,
        preferredLanguage: 'en',
        preferredTheme: 'light',
        isActive: user.isActive,
        groupIds: [],
        createdAt: user.createdAt,
      },
    ],
  })),
  on(AdminUsersActions.inviteUserFailure, (state, { error }): AdminUsersState => ({
    ...state,
    inviting: false,
    error,
  })),
  on(AdminUsersActions.loadGroups, (state): AdminUsersState => ({
    ...state,
    loadingGroups: true,
    error: null,
  })),
  on(AdminUsersActions.loadGroupsSuccess, (state, { groups }): AdminUsersState => ({
    ...state,
    groups,
    loadingGroups: false,
  })),
  on(AdminUsersActions.loadGroupsFailure, (state, { error }): AdminUsersState => ({
    ...state,
    loadingGroups: false,
    error,
  })),
  on(AdminUsersActions.createGroup, (state): AdminUsersState => ({
    ...state,
    creatingGroup: true,
    error: null,
    successMessageKey: null,
  })),
  on(AdminUsersActions.createGroupSuccess, (state, { group }): AdminUsersState => ({
    ...state,
    creatingGroup: false,
    groups: [...state.groups, group],
    successMessageKey: 'adminUsers.groups.createSuccess',
  })),
  on(AdminUsersActions.createGroupFailure, (state, { error }): AdminUsersState => ({
    ...state,
    creatingGroup: false,
    error,
  })),
  on(AdminUsersActions.selectGroup, (state, { groupId }): AdminUsersState => ({
    ...state,
    selectedGroupId: groupId,
    groupMembers: groupId === null ? [] : state.groupMembers,
  })),
  on(AdminUsersActions.loadGroupMembers, (state, { groupId }): AdminUsersState => ({
    ...state,
    selectedGroupId: groupId,
    loadingMembers: true,
    error: null,
  })),
  on(AdminUsersActions.loadGroupMembersSuccess, (state, { groupId, members }): AdminUsersState => ({
    ...state,
    selectedGroupId: groupId,
    groupMembers: members,
    loadingMembers: false,
  })),
  on(AdminUsersActions.loadGroupMembersFailure, (state, { error }): AdminUsersState => ({
    ...state,
    loadingMembers: false,
    error,
  })),
  on(AdminUsersActions.addGroupMember, (state): AdminUsersState => ({
    ...state,
    updatingMembers: true,
    error: null,
    successMessageKey: null,
  })),
  on(AdminUsersActions.addGroupMemberSuccess, (state, { groupId, member }): AdminUsersState => ({
    ...state,
    updatingMembers: false,
    groupMembers: [...state.groupMembers, member],
    groups: state.groups.map((g) =>
      g.id === groupId ? { ...g, memberCount: g.memberCount + 1 } : g,
    ),
    users: state.users.map((u) =>
      u.id === member.userId && !u.groupIds.includes(groupId)
        ? { ...u, groupIds: [...u.groupIds, groupId] }
        : u,
    ),
    successMessageKey: 'adminUsers.groups.memberAddedSuccess',
  })),
  on(AdminUsersActions.addGroupMemberFailure, (state, { error }): AdminUsersState => ({
    ...state,
    updatingMembers: false,
    error,
  })),
  on(AdminUsersActions.removeGroupMember, (state): AdminUsersState => ({
    ...state,
    updatingMembers: true,
    error: null,
    successMessageKey: null,
  })),
  on(AdminUsersActions.removeGroupMemberSuccess, (state, { groupId, userId }): AdminUsersState => ({
    ...state,
    updatingMembers: false,
    groupMembers: state.groupMembers.filter((m) => m.userId !== userId),
    groups: state.groups.map((g) =>
      g.id === groupId ? { ...g, memberCount: Math.max(0, g.memberCount - 1) } : g,
    ),
    users: state.users.map((u) =>
      u.id === userId ? { ...u, groupIds: u.groupIds.filter((id) => id !== groupId) } : u,
    ),
    successMessageKey: 'adminUsers.groups.memberRemovedSuccess',
  })),
  on(AdminUsersActions.removeGroupMemberFailure, (state, { error }): AdminUsersState => ({
    ...state,
    updatingMembers: false,
    error,
  })),
  on(AdminUsersActions.clearFeedback, (state): AdminUsersState => ({
    ...state,
    error: null,
    successMessageKey: null,
    lastInvitedUser: null,
  })),
);
