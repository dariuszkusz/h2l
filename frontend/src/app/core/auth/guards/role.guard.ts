import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { components } from '../../api/generated/openapi.types';
import { AuthActions } from '../store/auth.actions';
import { selectCurrentUserRole } from '../store/auth.selectors';

type SystemRole = components['schemas']['SystemRole'];

export function roleGuard(allowedRoles: readonly SystemRole[]): CanActivateFn {
  return () => {
    const store = inject(Store);
    const router = inject(Router);
    const role = store.selectSignal(selectCurrentUserRole)();

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
}
