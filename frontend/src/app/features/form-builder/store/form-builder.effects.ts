import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, switchMap } from 'rxjs';
import { FormsApiService } from '../../../core/api/forms-api.service';
import { ProblemDetailsMapperService } from '../../../core/api/services/problem-details-mapper.service';
import { FormBuilderActions } from './form-builder.actions';

@Injectable()
export class FormBuilderEffects {
  private readonly actions$ = inject(Actions);
  private readonly formsApi = inject(FormsApiService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);

  readonly loadForm$ = createEffect(() =>
    this.actions$.pipe(
      ofType(FormBuilderActions.loadForm),
      switchMap(({ formId }) =>
        this.formsApi.getFormDefinition(formId).pipe(
          map((form) => FormBuilderActions.loadFormSuccess({ form })),
          catchError((error: unknown) =>
            of(FormBuilderActions.loadFormFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        ),
      ),
    ),
  );

  readonly loadVersions$ = createEffect(() =>
    this.actions$.pipe(
      ofType(FormBuilderActions.loadVersions),
      switchMap(({ formId }) =>
        this.formsApi.listFormVersions(formId).pipe(
          map((versions) => FormBuilderActions.loadVersionsSuccess({ versions })),
          catchError((error: unknown) =>
            of(
              FormBuilderActions.loadVersionsFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly saveDraft$ = createEffect(() =>
    this.actions$.pipe(
      ofType(FormBuilderActions.saveDraft),
      exhaustMap(({ formId, request }) =>
        this.formsApi.updateFormDraft(formId, request).pipe(
          map((form) => FormBuilderActions.saveDraftSuccess({ form })),
          catchError((error: unknown) =>
            of(FormBuilderActions.saveDraftFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        ),
      ),
    ),
  );

  readonly publishVersion$ = createEffect(() =>
    this.actions$.pipe(
      ofType(FormBuilderActions.publishVersion),
      exhaustMap(({ formId }) =>
        this.formsApi.publishFormVersion(formId).pipe(
          map((version) => FormBuilderActions.publishVersionSuccess({ version })),
          catchError((error: unknown) =>
            of(
              FormBuilderActions.publishVersionFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );
}
