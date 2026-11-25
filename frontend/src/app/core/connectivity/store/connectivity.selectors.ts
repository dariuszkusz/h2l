import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ConnectivityState } from './connectivity.state';

export const selectConnectivityState = createFeatureSelector<ConnectivityState>('connectivity');

export const selectIsOnline = createSelector(
  selectConnectivityState,
  (state) => state.isOnline,
);

export const selectPendingDraftsCount = createSelector(
  selectConnectivityState,
  (state) => state.pendingDraftsCount,
);

export const selectSyncStatus = createSelector(
  selectConnectivityState,
  (state) => state.syncStatus,
);
