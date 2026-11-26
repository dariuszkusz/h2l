import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ProjectsApiService } from '../../../core/api/projects-api.service';
import { AdminProjectsActions } from './admin-projects.actions';
import { AdminProjectsEffects } from './admin-projects.effects';

describe('AdminProjectsEffects', () => {
  let actions$: Observable<Action>;
  let effects: AdminProjectsEffects;
  const projectsApiMock = {
    listProjects: vi.fn(),
    createProject: vi.fn(),
    listProjectProcesses: vi.fn(),
    createWorkflowProcess: vi.fn(),
    listProcessForms: vi.fn(),
    createFormDefinition: vi.fn(),
    getFormAccessRules: vi.fn(),
    updateFormAccessRules: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        AdminProjectsEffects,
        provideMockActions(() => actions$),
        { provide: ProjectsApiService, useValue: projectsApiMock },
      ],
    });
    effects = TestBed.inject(AdminProjectsEffects);
  });

  it('loads projects and maps 402 limit error on createProcess', async () => {
    projectsApiMock.listProjects.mockReturnValue(of([]));
    actions$ = of(AdminProjectsActions.loadProjects());
    expect(await firstValueFrom(effects.loadProjects$)).toEqual(
      AdminProjectsActions.loadProjectsSuccess({ projects: [] }),
    );

    projectsApiMock.createWorkflowProcess.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 402,
            error: { errorCode: 'FREE_PLAN_PROCESS_LIMIT_EXCEEDED' },
          }),
      ),
    );
    actions$ = of(
      AdminProjectsActions.createProcess({
        projectId: 'p1',
        request: { name: 'Second', externalProcessKey: 'second', status: 'Active' },
      }),
    );
    const failed = await firstValueFrom(effects.createProcess$);
    expect(failed.type).toBe(AdminProjectsActions.createProcessFailure.type);
  });

  it('saves ACL rules and triggers reloadAccessRulesOnSave', async () => {
    projectsApiMock.updateFormAccessRules.mockReturnValue(of(void 0));
    actions$ = of(
      AdminProjectsActions.saveAccessRules({
        formId: 'f1',
        request: { rules: [{ groupId: 'g1', userId: 'u1' }] },
      }),
    );
    expect(await firstValueFrom(effects.saveAccessRules$)).toEqual(
      AdminProjectsActions.saveAccessRulesSuccess({ formId: 'f1' }),
    );

    actions$ = of(AdminProjectsActions.saveAccessRulesSuccess({ formId: 'f1' }));
    expect(await firstValueFrom(effects.reloadAccessRulesOnSave$)).toEqual(
      AdminProjectsActions.loadAccessRules({ formId: 'f1' }),
    );
  });
});
