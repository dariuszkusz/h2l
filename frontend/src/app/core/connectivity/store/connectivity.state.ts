import { SyncStatus } from '../models/sync-status.type';

export interface ConnectivityState {
  readonly isOnline: boolean;
  readonly pendingDraftsCount: number;
  readonly syncStatus: SyncStatus;
}
