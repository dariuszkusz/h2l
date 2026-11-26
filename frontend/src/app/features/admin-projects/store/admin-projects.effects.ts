import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, switchMap } from 'rxjs';
import { ProjectsApiService } from '../../../core/api/projects-api.service';
import { ProblemDetailsMapperService } from '../../../core/api/services/problem-details-mapper.service';
import { AdminProjectsActions } from './admin-projects.actions';

@Injectable()
export class AdminProjectsEffects {
  private readonly actions$ = inject(Actions);
  private readonly projectsApi = inject(ProjectsApiService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);

  readonly loadProjects$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.loadProjects),
      switchMap(() =>
        this.projectsApi.listProjects().pipe(
          map((projects) => AdminProjectsActions.loadProjectsSuccess({ projects })),
          catchError((error: unknown) =>
            of(
              AdminProjectsActions.loadProjectsFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly createProject$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.createProject),
      exhaustMap(({ request }) =>
        this.projectsApi.createProject(request).pipe(
          map((project) => AdminProjectsActions.createProjectSuccess({ project })),
          catchError((error: unknown) =>
            of(
              AdminProjectsActions.createProjectFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly loadProcesses$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.loadProcesses),
      switchMap(({ projectId }) =>
        this.projectsApi.listProjectProcesses(projectId).pipe(
          map((processes) => AdminProjectsActions.loadProcessesSuccess({ projectId, processes })),
          catchError((error: unknown) =>
            of(
              AdminProjectsActions.loadProcessesFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly createProcess$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.createProcess),
      exhaustMap(({ projectId, request }) =>
        this.projectsApi.createWorkflowProcess(projectId, request).pipe(
          map((process) => AdminProjectsActions.createProcessSuccess({ process })),
          catchError((error: unknown) =>
            of(
              AdminProjectsActions.createProcessFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly loadForms$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.loadForms),
      switchMap(({ processId }) =>
        this.projectsApi.listProcessForms(processId).pipe(
          map((forms) => AdminProjectsActions.loadFormsSuccess({ processId, forms })),
          catchError((error: unknown) =>
            of(
              AdminProjectsActions.loadFormsFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly createForm$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.createForm),
      exhaustMap(({ processId, request }) =>
        this.projectsApi.createFormDefinition(processId, request).pipe(
          map((form) => AdminProjectsActions.createFormSuccess({ form })),
          catchError((error: unknown) =>
            of(
              AdminProjectsActions.createFormFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly loadAccessRules$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.loadAccessRules),
      switchMap(({ formId }) =>
        this.projectsApi.getFormAccessRules(formId).pipe(
          map((response) =>
            AdminProjectsActions.loadAccessRulesSuccess({
              formId: response.formId,
              rules: response.rules,
            }),
          ),
          catchError((error: unknown) =>
            of(
              AdminProjectsActions.loadAccessRulesFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly saveAccessRules$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.saveAccessRules),
      exhaustMap(({ formId, request }) =>
        this.projectsApi.updateFormAccessRules(formId, request).pipe(
          map(() => AdminProjectsActions.saveAccessRulesSuccess({ formId })),
          catchError((error: unknown) =>
            of(
              AdminProjectsActions.saveAccessRulesFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly reloadAccessRulesOnSave$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminProjectsActions.saveAccessRulesSuccess),
      map(({ formId }) => AdminProjectsActions.loadAccessRules({ formId })),
    ),
  );
}
