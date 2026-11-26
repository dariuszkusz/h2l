import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from '../../api/api-base-url.token';
import { AuthApiService } from '../../api/auth-api.service';
import { ProblemDetailsMapperService } from '../../api/services/problem-details-mapper.service';
import { AuthTokenStorageService } from '../services/auth-token-storage.service';
import { JwtRefreshCoordinatorService } from '../services/jwt-refresh-coordinator.service';
import { AuthActions } from '../store/auth.actions';

const PUBLIC_ENDPOINT_PATHS: readonly string[] = [
  '/setup/status',
  '/setup/initialize',
  '/auth/login',
  '/auth/refresh',
  '/auth/activate',
];

function isPublicApiEndpoint(url: string, baseUrl: string): boolean {
  return PUBLIC_ENDPOINT_PATHS.some(
    (path) => url.startsWith(`${baseUrl}${path}`) || url.endsWith(path),
  );
}

function isProtectedApiRequest(url: string, baseUrl: string): boolean {
  return url.startsWith(baseUrl) && !isPublicApiEndpoint(url, baseUrl);
}

function attachBearerToken(
  req: HttpRequest<unknown>,
  accessToken: string | null,
): HttpRequest<unknown> {
  if (!accessToken || req.headers.has('Authorization')) {
    return req;
  }

  return req.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`,
    },
  });
}

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const baseUrl = inject(API_BASE_URL);
  const tokenStorage = inject(AuthTokenStorageService);
  const authApi = inject(AuthApiService);
  const refreshCoordinator = inject(JwtRefreshCoordinatorService);
  const problemDetailsMapper = inject(ProblemDetailsMapperService);
  const store = inject(Store);

  if (!isProtectedApiRequest(req.url, baseUrl)) {
    return next(req);
  }

  const authorizedReq = attachBearerToken(req, tokenStorage.getAccessToken());

  return next(authorizedReq).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status === 403) {
        store.dispatch(
          AuthActions.forbiddenEncountered({
            error: problemDetailsMapper.map(error),
          }),
        );
        return throwError(() => error);
      }

      if (error.status !== 401) {
        return throwError(() => error);
      }

      const refreshToken = tokenStorage.getRefreshToken();
      if (!refreshToken) {
        store.dispatch(AuthActions.sessionExpired());
        return throwError(() => error);
      }

      return refreshCoordinator
        .refreshOrJoin(() =>
          authApi.refresh({ refreshToken }).pipe(
            switchMap((response) => {
              tokenStorage.setTokens(response.accessToken, response.refreshToken);
              store.dispatch(AuthActions.refreshTokenSuccess({ response }));
              return [response.accessToken];
            }),
            catchError((refreshError: unknown) => {
              store.dispatch(AuthActions.sessionExpired());
              return throwError(() => refreshError);
            }),
          ),
        )
        .pipe(
          switchMap((newAccessToken) =>
            next(
              req.clone({
                setHeaders: {
                  Authorization: `Bearer ${newAccessToken}`,
                },
              }),
            ),
          ),
        );
    }),
  );
};
