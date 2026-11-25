import { describe, expect, it } from 'vitest';
import {
  selectIsOnline,
  selectPendingDraftsCount,
  selectSyncStatus,
} from './connectivity.selectors';
import { ConnectivityState } from './connectivity.state';

describe('connectivity selectors', () => {
  const mockState: ConnectivityState = {
    isOnline: false,
    pendingDraftsCount: 3,
    syncStatus: 'queued',
  };

  it('selects online status', () => {
    expect(selectIsOnline.projector(mockState)).toBe(false);
  });

  it('selects pending drafts count', () => {
    expect(selectPendingDraftsCount.projector(mockState)).toBe(3);
  });

  it('selects sync status', () => {
    expect(selectSyncStatus.projector(mockState)).toBe('queued');
  });
});
