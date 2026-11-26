import { AdminProjectsActions } from './admin-projects.actions';
import { adminProjectsReducer, initialAdminProjectsState } from './admin-projects.reducer';

describe('adminProjectsReducer', () => {
  it('stores projects, processes, forms, and access rules', () => {
    const withProject = adminProjectsReducer(
      initialAdminProjectsState,
      AdminProjectsActions.createProjectSuccess({
        project: { id: 'p1', name: 'HR', description: null, createdAt: '2026-11-25T20:00:00Z' },
      }),
    );
    expect(withProject.projects).toHaveLength(1);

    const withProcess = adminProjectsReducer(
      withProject,
      AdminProjectsActions.createProcessSuccess({
        process: {
          id: 'proc1',
          projectId: 'p1',
          name: 'Software Purchase',
          externalProcessKey: 'soft-purchase',
          status: 'Active',
          createdAt: '2026-11-25T20:00:00Z',
        },
      }),
    );
    expect(withProcess.processes).toHaveLength(1);

    const withRules = adminProjectsReducer(
      withProcess,
      AdminProjectsActions.loadAccessRulesSuccess({
        formId: 'f1',
        rules: [{ id: 'r1', groupId: 'g1', userId: null, accessScope: 'WholeGroup' }],
      }),
    );
    expect(withRules.accessRules).toHaveLength(1);
    expect(withRules.selectedFormId).toBe('f1');
  });

  it('clears child selections when switching project', () => {
    const populated = {
      ...initialAdminProjectsState,
      selectedProjectId: 'p1',
      selectedProcessId: 'proc1',
      selectedFormId: 'f1',
    };
    const switched = adminProjectsReducer(
      populated,
      AdminProjectsActions.selectProject({ projectId: 'p2' }),
    );
    expect(switched.selectedProjectId).toBe('p2');
    expect(switched.selectedProcessId).toBeNull();
    expect(switched.selectedFormId).toBeNull();
  });
});
