import { createReducer, on } from '@ngrx/store';
import { ConnectivityActions } from './connectivity.actions';
import { ConnectivityState } from './connectivity.state';

export const initialConnectivityState: ConnectivityState = {
  isOnline: true,
  pendingDraftsCount: 0,
  syncStatus: 'idle',
};

export const connectivityReducer = createReducer(
  initialConnectivityState,
  on(ConnectivityActions.statusChanged, (state, { isOnline }): ConnectivityState => ({
    ...state,
    isOnline,
    syncStatus: !isOnline && state.pendingDraftsCount > 0 ? 'queued' : state.syncStatus,
  })),
  on(
    ConnectivityActions.pendingDraftsUpdated,
    (state, { pendingDraftsCount }): ConnectivityState => ({
      ...state,
      pendingDraftsCount,
      syncStatus: pendingDraftsCount > 0 && !state.isOnline ? 'queued' : state.syncStatus,
    }),
  ),
  on(ConnectivityActions.syncStatusChanged, (state, { syncStatus }): ConnectivityState => ({
    ...state,
    syncStatus,
  })),
);
