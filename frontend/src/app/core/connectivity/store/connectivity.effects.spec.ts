import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action, provideStore, Store } from '@ngrx/store';
import { firstValueFrom, Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { ConnectivityActions } from './connectivity.actions';
import { ConnectivityEffects } from './connectivity.effects';
import { connectivityReducer } from './connectivity.reducer';

describe('ConnectivityEffects', () => {
  let actions$: Observable<Action>;
  let effects: ConnectivityEffects;
  let store: Store;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ConnectivityEffects,
        provideStore({ connectivity: connectivityReducer }),
        provideMockActions(() => actions$),
      ],
    });
    effects = TestBed.inject(ConnectivityEffects);
    store = TestBed.inject(Store);
  });

  it('triggers syncing status when reconnecting with pending drafts', async () => {
    store.dispatch(ConnectivityActions.pendingDraftsUpdated({ pendingDraftsCount: 2 }));
    actions$ = of(ConnectivityActions.statusChanged({ isOnline: true }));

    const result = await firstValueFrom(effects.triggerDraftSyncOnReconnect$);
    expect(result).toEqual(ConnectivityActions.syncStatusChanged({ syncStatus: 'syncing' }));
  });
});
