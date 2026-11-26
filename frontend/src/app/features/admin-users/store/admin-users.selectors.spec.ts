import {
  selectAdminGroupsList,
  selectAdminUsersList,
  selectSelectedGroup,
} from './admin-users.selectors';
import { initialAdminUsersState } from './admin-users.reducer';

describe('adminUsers selectors', () => {
  it('selects users, groups, and active selected group', () => {
    const groups = [
      { id: 'g1', name: 'HR', code: 'HR', memberCount: 1, createdAt: '2026-11-25T20:00:00Z' },
    ];
    const state = {
      ...initialAdminUsersState,
      groups,
      selectedGroupId: 'g1',
    };

    expect(selectAdminUsersList.projector(state)).toEqual([]);
    expect(selectAdminGroupsList.projector(state)).toEqual(groups);
    expect(selectSelectedGroup.projector(groups, 'g1')?.code).toBe('HR');
    expect(selectSelectedGroup.projector(groups, 'missing')).toBeNull();
  });
});
