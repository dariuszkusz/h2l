import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, of, switchMap } from 'rxjs';
import { LicensingApiService } from '../../api/licensing-api.service';
import { ProblemDetailsMapperService } from '../../api/services/problem-details-mapper.service';
import { LicensingActions } from './licensing.actions';

@Injectable()
export class LicensingEffects {
  private readonly actions$ = inject(Actions);
  private readonly licensingApi = inject(LicensingApiService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);

  readonly loadStatus$ = createEffect(() =>
    this.actions$.pipe(
      ofType(LicensingActions.loadStatus),
      switchMap(() =>
        this.licensingApi.getStatus().pipe(
          map((status) => LicensingActions.loadStatusSuccess({ status })),
          catchError((error: unknown) =>
            of(LicensingActions.loadStatusFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        ),
      ),
    ),
  );

  readonly activateLicense$ = createEffect(() =>
    this.actions$.pipe(
      ofType(LicensingActions.activateLicense),
      exhaustMap(({ request }) =>
        this.licensingApi.activate(request).pipe(
          map((status) => LicensingActions.activateLicenseSuccess({ status })),
          catchError((error: unknown) =>
            of(
              LicensingActions.activateLicenseFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );
}
