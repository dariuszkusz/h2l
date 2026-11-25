import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { SyncStatus } from '../models/sync-status.type';

export const ConnectivityActions = createActionGroup({
  source: 'Connectivity',
  events: {
    Initialize: emptyProps(),
    'Status Changed': props<{ isOnline: boolean }>(),
    'Pending Drafts Updated': props<{ pendingDraftsCount: number }>(),
    'Sync Status Changed': props<{ syncStatus: SyncStatus }>(),
  },
});
