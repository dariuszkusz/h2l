import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ConnectorsState } from './connectors.state';

export const selectConnectorsState = createFeatureSelector<ConnectorsState>('connectors');

export const selectConnectorsList = createSelector(
  selectConnectorsState,
  (state) => state.connectors,
);

export const selectActiveConnectorsCount = createSelector(
  selectConnectorsList,
  (connectors) => connectors.filter((c) => c.isActive).length,
);

export const selectDeadLettersList = createSelector(
  selectConnectorsState,
  (state) => state.deadLetters,
);

export const selectNeedsAssignmentTasks = createSelector(
  selectConnectorsState,
  (state) => state.needsAssignmentTasks,
);

export const selectConnectorsLoading = createSelector(
  selectConnectorsState,
  (state) => state.loadingConnectors,
);

export const selectDeadLettersLoading = createSelector(
  selectConnectorsState,
  (state) => state.loadingDeadLetters,
);

export const selectNeedsAssignmentLoading = createSelector(
  selectConnectorsState,
  (state) => state.loadingNeedsAssignment,
);

export const selectConnectorsSaving = createSelector(
  selectConnectorsState,
  (state) => state.saving,
);

export const selectTestingConnectorId = createSelector(
  selectConnectorsState,
  (state) => state.testingConnectorId,
);

export const selectConnectorTestResults = createSelector(
  selectConnectorsState,
  (state) => state.testResults,
);

export const selectRetryingOutboxId = createSelector(
  selectConnectorsState,
  (state) => state.retryingOutboxId,
);

export const selectAssigningTaskId = createSelector(
  selectConnectorsState,
  (state) => state.assigningTaskId,
);

export const selectConnectorsError = createSelector(
  selectConnectorsState,
  (state) => state.error,
);

export const selectConnectorsSuccessKey = createSelector(
  selectConnectorsState,
  (state) => state.successKey,
);
