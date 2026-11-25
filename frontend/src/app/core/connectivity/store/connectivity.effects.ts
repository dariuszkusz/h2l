import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { filter, map, switchMap, withLatestFrom } from 'rxjs';
import { ConnectivityMonitorService } from '../services/connectivity-monitor.service';
import { ConnectivityActions } from './connectivity.actions';
import { selectPendingDraftsCount } from './connectivity.selectors';

@Injectable()
export class ConnectivityEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store);
  private readonly connectivityMonitor = inject(ConnectivityMonitorService);

  readonly monitorConnectivity$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectivityActions.initialize),
      switchMap(() =>
        this.connectivityMonitor.isOnline$.pipe(
          map((isOnline) => ConnectivityActions.statusChanged({ isOnline })),
        ),
      ),
    ),
  );

  readonly triggerDraftSyncOnReconnect$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectivityActions.statusChanged),
      withLatestFrom(this.store.select(selectPendingDraftsCount)),
      filter(([{ isOnline }, pendingDraftsCount]) => isOnline && pendingDraftsCount > 0),
      map(() => ConnectivityActions.syncStatusChanged({ syncStatus: 'syncing' })),
    ),
  );
}
