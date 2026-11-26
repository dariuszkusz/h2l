import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { filter, map, take } from 'rxjs';
import { AuthTokenStorageService } from '../services/auth-token-storage.service';
import { AuthActions } from '../store/auth.actions';
import { selectAuthState } from '../store/auth.selectors';

export const authGuard: CanActivateFn = (_route, state) => {
  const store = inject(Store);
  const router = inject(Router);
  const tokenStorage = inject(AuthTokenStorageService);

  const current = store.selectSignal(selectAuthState)();
  if (current.isAuthenticated) {
    return true;
  }

  const hasStoredToken =
    tokenStorage.getAccessToken() !== null || tokenStorage.getRefreshToken() !== null;

  if (!hasStoredToken) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url },
    });
  }

  if (!current.hydrated && !current.loading) {
    store.dispatch(AuthActions.hydrateSession());
  }

  return store.select(selectAuthState).pipe(
    filter((authState) => authState.hydrated && !authState.loading),
    take(1),
    map((authState) =>
      authState.isAuthenticated
        ? true
        : router.createUrlTree(['/login'], {
            queryParams: { returnUrl: state.url },
          }),
    ),
  );
};
