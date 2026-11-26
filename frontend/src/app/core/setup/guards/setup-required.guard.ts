import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { filter, map, take } from 'rxjs';
import { SetupActions } from '../store/setup.actions';
import { selectSetupState } from '../store/setup.selectors';

export const setupRequiredGuard: CanActivateFn = () => {
  const store = inject(Store);
  const router = inject(Router);

  const initialState = store.selectSignal(selectSetupState)();
  if (initialState.isInitialized === null && !initialState.loadingStatus) {
    store.dispatch(SetupActions.checkStatus());
  }

  return store.select(selectSetupState).pipe(
    filter((state) => !state.loadingStatus && (state.isInitialized !== null || state.error !== null)),
    take(1),
    map((state) => (state.isInitialized === false ? router.createUrlTree(['/setup']) : true)),
  );
};
