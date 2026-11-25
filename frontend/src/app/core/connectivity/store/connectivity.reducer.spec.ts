import { describe, expect, it } from 'vitest';
import { ConnectivityActions } from './connectivity.actions';
import { connectivityReducer, initialConnectivityState } from './connectivity.reducer';

describe('connectivityReducer', () => {
  it('updates online status and marks queued when offline with pending drafts', () => {
    const withDrafts = connectivityReducer(
      initialConnectivityState,
      ConnectivityActions.pendingDraftsUpdated({ pendingDraftsCount: 2 }),
    );
    const offlineState = connectivityReducer(
      withDrafts,
      ConnectivityActions.statusChanged({ isOnline: false }),
    );
    expect(offlineState.isOnline).toBe(false);
    expect(offlineState.pendingDraftsCount).toBe(2);
    expect(offlineState.syncStatus).toBe('queued');
  });

  it('updates sync status explicitly', () => {
    const syncing = connectivityReducer(
      initialConnectivityState,
      ConnectivityActions.syncStatusChanged({ syncStatus: 'syncing' }),
    );
    expect(syncing.syncStatus).toBe('syncing');
  });
});
