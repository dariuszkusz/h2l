import { createFeatureSelector, createSelector } from '@ngrx/store';
import { TasksState } from './tasks.state';

export const selectTasksState = createFeatureSelector<TasksState>('tasks');

export const selectTasksItems = createSelector(
  selectTasksState,
  (state) => state.items,
);

export const selectTasksTotalCount = createSelector(
  selectTasksState,
  (state) => state.totalCount,
);

export const selectTasksScope = createSelector(
  selectTasksState,
  (state) => state.scope,
);

export const selectSelectedTask = createSelector(
  selectTasksState,
  (state) => state.selectedTask,
);

export const selectTaskLocalDraftValues = createSelector(
  selectTasksState,
  (state) => state.localDraftValues,
);

export const selectQueuedTaskIds = createSelector(
  selectTasksState,
  (state) => state.queuedTaskIds,
);

export const selectUploadedAttachments = createSelector(
  selectTasksState,
  (state) => state.uploadedAttachments,
);

export const selectTasksLoadingList = createSelector(
  selectTasksState,
  (state) => state.loadingList,
);

export const selectTasksLoadingDetail = createSelector(
  selectTasksState,
  (state) => state.loadingDetail,
);

export const selectTasksMutating = createSelector(
  selectTasksState,
  (state) => state.mutating,
);

export const selectTasksError = createSelector(
  selectTasksState,
  (state) => state.error,
);

export const selectTasksSuccessKey = createSelector(
  selectTasksState,
  (state) => state.successKey,
);
