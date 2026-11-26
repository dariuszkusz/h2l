import { AdminUsersActions } from './admin-users.actions';
import { adminUsersReducer, initialAdminUsersState } from './admin-users.reducer';

describe('adminUsersReducer', () => {
  it('handles inviteUserSuccess with manual_link delivery', () => {
    const invited = {
      id: 'u1',
      email: 'new@example.com',
      externalId: null,
      systemRole: 'EndUser' as const,
      isActive: false,
      deliveryMethod: 'manual_link' as const,
      activationLink: 'http://localhost:4200/activate?token=abc',
      createdAt: '2026-11-25T20:00:00Z',
    };

    const next = adminUsersReducer(
      initialAdminUsersState,
      AdminUsersActions.inviteUserSuccess({ user: invited }),
    );
    expect(next.users).toHaveLength(1);
    expect(next.lastInvitedUser?.activationLink).toContain('token=abc');
    expect(next.successMessageKey).toBe('adminUsers.invite.successManualLink');
  });

  it('handles createGroupSuccess, addGroupMemberSuccess, and removeGroupMemberSuccess', () => {
    const withGroup = adminUsersReducer(
      initialAdminUsersState,
      AdminUsersActions.createGroupSuccess({
        group: {
          id: 'g1',
          name: 'HR',
          code: 'HR',
          memberCount: 0,
          createdAt: '2026-11-25T20:00:00Z',
        },
      }),
    );
    expect(withGroup.groups).toHaveLength(1);

    const withMember = adminUsersReducer(
      withGroup,
      AdminUsersActions.addGroupMemberSuccess({
        groupId: 'g1',
        member: {
          userId: 'u1',
          email: 'u1@example.com',
          systemRole: 'EndUser',
          isActive: true,
        },
      }),
    );
    expect(withMember.groupMembers).toHaveLength(1);
    expect(withMember.groups[0].memberCount).toBe(1);

    const removed = adminUsersReducer(
      withMember,
      AdminUsersActions.removeGroupMemberSuccess({ groupId: 'g1', userId: 'u1' }),
    );
    expect(removed.groupMembers).toHaveLength(0);
    expect(removed.groups[0].memberCount).toBe(0);
  });
});
