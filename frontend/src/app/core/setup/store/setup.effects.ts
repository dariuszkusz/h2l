import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, switchMap, tap } from 'rxjs';
import { ProblemDetailsMapperService } from '../../api/services/problem-details-mapper.service';
import { SetupApiService } from '../../api/setup-api.service';
import { I18nActions } from '../../i18n/store/i18n.actions';
import { ThemeActions } from '../../theme/store/theme.actions';
import { SetupActions } from './setup.actions';

@Injectable()
export class SetupEffects {
  private readonly actions$ = inject(Actions);
  private readonly setupApi = inject(SetupApiService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);
  private readonly router = inject(Router);

  readonly checkStatus$ = createEffect(() =>
    this.actions$.pipe(
      ofType(SetupActions.checkStatus),
      switchMap(() =>
        this.setupApi.getStatus().pipe(
          map((status) => SetupActions.checkStatusSuccess({ status })),
          catchError((error: unknown) =>
            of(SetupActions.checkStatusFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        ),
      ),
    ),
  );

  readonly initialize$ = createEffect(() =>
    this.actions$.pipe(
      ofType(SetupActions.initialize),
      exhaustMap(({ request }) =>
        this.setupApi.initialize(request).pipe(
          map((response) =>
            SetupActions.initializeSuccess({
              response,
              language: request.defaultLanguage,
              palette: request.selectedPalette,
            }),
          ),
          catchError((error: unknown) => {
            const mapped = this.problemDetailsMapper.map(error);
            if (mapped.status === 409) {
              return of(SetupActions.initializeAlreadyDone({ error: mapped }));
            }
            return of(SetupActions.initializeFailure({ error: mapped }));
          }),
        ),
      ),
    ),
  );

  readonly applyLanguageOnInitialize$ = createEffect(() =>
    this.actions$.pipe(
      ofType(SetupActions.initializeSuccess),
      map(({ language }) => I18nActions.changeLanguage({ language })),
    ),
  );

  readonly applyPaletteOnInitialize$ = createEffect(() =>
    this.actions$.pipe(
      ofType(SetupActions.initializeSuccess),
      map(({ palette }) => ThemeActions.setColorPalette({ palette })),
    ),
  );

  readonly redirectAfterSetup$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(SetupActions.initializeSuccess, SetupActions.initializeAlreadyDone),
        tap(() => {
          void this.router.navigate(['/login']);
        }),
      ),
    { dispatch: false },
  );
}
