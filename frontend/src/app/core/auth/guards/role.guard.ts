import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { Store } from '@ngrx/store';
import { filter, map, take } from 'rxjs';
import { components } from '../../api/generated/openapi.types';
import { AuthTokenStorageService } from '../services/auth-token-storage.service';
import { AuthActions } from '../store/auth.actions';
import { selectAuthState } from '../store/auth.selectors';

type SystemRole = components['schemas']['SystemRole'];

export function roleGuard(allowedRoles: readonly SystemRole[]): CanActivateFn {
  return () => {
    const store = inject(Store);
    const router = inject(Router);
    const tokenStorage = inject(AuthTokenStorageService);

    const evaluateRole = (role: SystemRole | null): boolean | UrlTree => {
      if (role !== null && allowedRoles.includes(role)) {
        return true;
      }

      store.dispatch(
        AuthActions.forbiddenEncountered({
          error: {
            status: 403,
            errorCode: 'FORBIDDEN',
            messageKey: 'errors.http.forbidden',
            detail: null,
            fieldErrors: {},
            isOffline: false,
          },
        }),
      );

      return router.createUrlTree(['/tasks']);
    };

    const current = store.selectSignal(selectAuthState)();
    if (current.isAuthenticated) {
      return evaluateRole(current.user?.systemRole ?? null);
    }

    const hasStoredToken =
      tokenStorage.getAccessToken() !== null || tokenStorage.getRefreshToken() !== null;

    if (!hasStoredToken) {
      return evaluateRole(null);
    }

    return store.select(selectAuthState).pipe(
      filter((authState) => authState.hydrated && !authState.loading),
      take(1),
      map((authState) => evaluateRole(authState.user?.systemRole ?? null)),
    );
  };
}
