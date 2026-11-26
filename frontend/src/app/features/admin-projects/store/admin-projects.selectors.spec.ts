import { initialAdminProjectsState } from './admin-projects.reducer';
import {
  selectSelectedForm,
  selectSelectedProcess,
  selectSelectedProject,
} from './admin-projects.selectors';

describe('adminProjects selectors', () => {
  it('resolves selected project, process, and form', () => {
    const project = {
      id: 'p1',
      name: 'HR',
      description: null,
      createdAt: '2026-11-25T20:00:00Z',
    };
    const process = {
      id: 'proc1',
      projectId: 'p1',
      name: 'Purchase',
      externalProcessKey: 'purchase',
      status: 'Active' as const,
      createdAt: '2026-11-25T20:00:00Z',
    };
    const form = {
      id: 'f1',
      workflowProcessId: 'proc1',
      name: 'Approval',
      stepKey: 'approval',
      status: 'Draft' as const,
      updatedAt: '2026-11-25T20:00:00Z',
    };

    expect(selectSelectedProject.projector([project], 'p1')?.name).toBe('HR');
    expect(selectSelectedProcess.projector([process], 'proc1')?.externalProcessKey).toBe(
      'purchase',
    );
    expect(selectSelectedForm.projector([form], 'f1')?.stepKey).toBe('approval');
    expect(selectSelectedForm.projector(initialAdminProjectsState.forms, 'f1')).toBeNull();
  });
});
