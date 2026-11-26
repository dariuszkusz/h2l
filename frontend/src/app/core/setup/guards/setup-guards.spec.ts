import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { provideStore, Store } from '@ngrx/store';
import { firstValueFrom, Observable } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { authReducer } from '../../auth/store/auth.reducer';
import { SetupActions } from '../store/setup.actions';
import { setupReducer } from '../store/setup.reducer';
import { setupNotInitializedGuard } from './setup-not-initialized.guard';
import { setupRequiredGuard } from './setup-required.guard';

const MOCK_ROUTE = {} as ActivatedRouteSnapshot;
const MOCK_STATE = { url: '/login' } as RouterStateSnapshot;

describe('Setup Route Guards', () => {
  let store: Store;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideStore({ setup: setupReducer, auth: authReducer }),
      ],
    });
    store = TestBed.inject(Store);
  });

  it('setupRequiredGuard redirects to /setup when isInitialized is false', async () => {
    store.dispatch(
      SetupActions.checkStatusSuccess({
        status: { isInitialized: false, instanceId: 'inst-1' },
      }),
    );

    const guard$ = TestBed.runInInjectionContext(() =>
      setupRequiredGuard(MOCK_ROUTE, MOCK_STATE),
    ) as Observable<boolean | UrlTree>;

    const result = await firstValueFrom(guard$);
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/setup');
  });

  it('setupRequiredGuard allows navigation when isInitialized is true', async () => {
    store.dispatch(
      SetupActions.checkStatusSuccess({
        status: { isInitialized: true, instanceId: 'inst-1' },
      }),
    );

    const guard$ = TestBed.runInInjectionContext(() =>
      setupRequiredGuard(MOCK_ROUTE, MOCK_STATE),
    ) as Observable<boolean | UrlTree>;

    expect(await firstValueFrom(guard$)).toBe(true);
  });

  it('setupNotInitializedGuard redirects /setup to /login when already initialized', async () => {
    store.dispatch(
      SetupActions.checkStatusSuccess({
        status: { isInitialized: true, instanceId: 'inst-1' },
      }),
    );

    const guard$ = TestBed.runInInjectionContext(() =>
      setupNotInitializedGuard(MOCK_ROUTE, MOCK_STATE),
    ) as Observable<boolean | UrlTree>;

    const result = await firstValueFrom(guard$);
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/login');
  });
});
