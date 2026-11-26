import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, from, map, of, switchMap, tap } from 'rxjs';
import { AuthApiService } from '../../api/auth-api.service';
import { ProblemDetailsMapperService } from '../../api/services/problem-details-mapper.service';
import { UsersApiService } from '../../api/users-api.service';
import { OfflineDraftStorageService } from '../../connectivity/services/offline-draft-storage.service';
import { I18nActions } from '../../i18n/store/i18n.actions';
import { ThemeActions } from '../../theme/store/theme.actions';
import { AuthTokenStorageService } from '../services/auth-token-storage.service';
import { AuthActions } from './auth.actions';

@Injectable()
export class AuthEffects {
  private readonly actions$ = inject(Actions);
  private readonly authApi = inject(AuthApiService);
  private readonly usersApi = inject(UsersApiService);
  private readonly tokenStorage = inject(AuthTokenStorageService);
  private readonly draftStorage = inject(OfflineDraftStorageService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);
  private readonly router = inject(Router);

  readonly login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.login),
      exhaustMap(({ request, returnUrl }) =>
        this.authApi.login(request).pipe(
          map((response) => {
            this.tokenStorage.setTokens(response.accessToken, response.refreshToken);
            return AuthActions.loginSuccess({ response, returnUrl });
          }),
          catchError((error: unknown) =>
            of(AuthActions.loginFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        ),
      ),
    ),
  );

  readonly syncLanguageOnAuth$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.loginSuccess, AuthActions.hydrateSessionSuccess, AuthActions.refreshTokenSuccess),
      map((action) => {
        const user = 'response' in action ? action.response.user : action.user;
        return I18nActions.changeLanguage({ language: user.preferredLanguage });
      }),
    ),
  );

  readonly syncThemeOnAuth$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.loginSuccess, AuthActions.hydrateSessionSuccess, AuthActions.refreshTokenSuccess),
      map((action) => {
        const user = 'response' in action ? action.response.user : action.user;
        return ThemeActions.setThemeMode({ mode: user.preferredTheme });
      }),
    ),
  );

  readonly navigateOnLogin$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.loginSuccess),
        tap(({ returnUrl }) => {
          const target = this.resolveSafeReturnUrl(returnUrl);
          void this.router.navigateByUrl(target);
        }),
      ),
    { dispatch: false },
  );

  readonly hydrateSession$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.hydrateSession),
      switchMap(() => {
        const accessToken = this.tokenStorage.getAccessToken();
        const refreshToken = this.tokenStorage.getRefreshToken();

        if (!accessToken && !refreshToken) {
          return of(AuthActions.hydrateSessionSkipped());
        }

        return this.usersApi.getMe().pipe(
          map((user) =>
            AuthActions.hydrateSessionSuccess({
              user,
              accessToken: this.tokenStorage.getAccessToken() ?? accessToken ?? '',
              refreshToken: this.tokenStorage.getRefreshToken() ?? refreshToken ?? '',
            }),
          ),
          catchError((error: unknown) => {
            const mapped = this.problemDetailsMapper.map(error);
            if (mapped.status === 401) {
              this.tokenStorage.clearTokens();
            }
            return of(AuthActions.hydrateSessionFailure({ error: mapped }));
          }),
        );
      }),
    ),
  );

  readonly refreshToken$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.refreshToken),
      exhaustMap(() => {
        const refreshToken = this.tokenStorage.getRefreshToken();
        if (!refreshToken) {
          return of(AuthActions.sessionExpired());
        }

        return this.authApi.refresh({ refreshToken }).pipe(
          map((response) => {
            this.tokenStorage.setTokens(response.accessToken, response.refreshToken);
            return AuthActions.refreshTokenSuccess({ response });
          }),
          catchError((error: unknown) =>
            of(
              AuthActions.refreshTokenFailure({
                error: this.problemDetailsMapper.map(error),
              }),
              AuthActions.sessionExpired(),
            ),
          ),
        );
      }),
    ),
  );

  readonly logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.logout),
      exhaustMap(() => {
        const refreshToken = this.tokenStorage.getRefreshToken();
        const revoke$ = refreshToken
          ? this.authApi.logout({ refreshToken }).pipe(catchError(() => of(void 0)))
          : of(void 0);

        return revoke$.pipe(
          switchMap(() => {
            this.tokenStorage.clearTokens();
            return from(this.draftStorage.clearAllDrafts()).pipe(catchError(() => of(void 0)));
          }),
          tap(() => {
            void this.router.navigate(['/login']);
          }),
          map(() => AuthActions.logoutCompleted()),
        );
      }),
    ),
  );

  readonly handleSessionExpired$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.sessionExpired),
        switchMap(() => {
          this.tokenStorage.clearTokens();
          return from(this.draftStorage.clearAllDrafts()).pipe(catchError(() => of(void 0)));
        }),
        tap(() => {
          void this.router.navigate(['/login']);
        }),
      ),
    { dispatch: false },
  );

  readonly activateAccount$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.activateAccount),
      exhaustMap(({ request }) =>
        this.authApi.activate(request).pipe(
          map(() => AuthActions.activateAccountSuccess()),
          catchError((error: unknown) =>
            of(
              AuthActions.activateAccountFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly updatePreferences$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.updatePreferences),
      switchMap(({ request }) =>
        this.usersApi.updateMyPreferences(request).pipe(
          map(() => AuthActions.updatePreferencesSuccess({ request })),
          catchError((error: unknown) =>
            of(
              AuthActions.updatePreferencesFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  private resolveSafeReturnUrl(returnUrl: string | null | undefined): string {
    if (!returnUrl || !returnUrl.startsWith('/') || returnUrl.startsWith('//')) {
      return '/tasks';
    }

    if (
      returnUrl.startsWith('/login') ||
      returnUrl.startsWith('/setup') ||
      returnUrl.startsWith('/activate')
    ) {
      return '/tasks';
    }

    return returnUrl;
  }
}
