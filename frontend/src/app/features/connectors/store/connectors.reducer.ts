import { createReducer, on } from '@ngrx/store';
import { ConnectorsActions } from './connectors.actions';
import { initialConnectorsState } from './connectors.state';

export const connectorsReducer = createReducer(
  initialConnectorsState,

  on(ConnectorsActions.loadConnectors, (state) => ({
    ...state,
    loadingConnectors: true,
    error: null,
  })),
  on(ConnectorsActions.loadConnectorsSuccess, (state, { connectors }) => ({
    ...state,
    connectors,
    loadingConnectors: false,
    error: null,
  })),
  on(ConnectorsActions.loadConnectorsFailure, (state, { error }) => ({
    ...state,
    loadingConnectors: false,
    error,
  })),

  on(ConnectorsActions.createConnector, (state) => ({
    ...state,
    saving: true,
    error: null,
    successKey: null,
  })),
  on(ConnectorsActions.createConnectorSuccess, (state, { connector }) => ({
    ...state,
    connectors: [connector, ...state.connectors],
    saving: false,
    error: null,
    successKey: 'connectors.feedback.created',
  })),
  on(ConnectorsActions.createConnectorFailure, (state, { error }) => ({
    ...state,
    saving: false,
    error,
  })),

  on(ConnectorsActions.updateConnector, (state) => ({
    ...state,
    saving: true,
    error: null,
    successKey: null,
  })),
  on(ConnectorsActions.updateConnectorSuccess, (state, { connector }) => ({
    ...state,
    connectors: state.connectors.map((item) => (item.id === connector.id ? connector : item)),
    saving: false,
    error: null,
    successKey: 'connectors.feedback.updated',
  })),
  on(ConnectorsActions.updateConnectorFailure, (state, { error }) => ({
    ...state,
    saving: false,
    error,
  })),

  on(ConnectorsActions.deleteConnector, (state) => ({
    ...state,
    saving: true,
    error: null,
    successKey: null,
  })),
  on(ConnectorsActions.deleteConnectorSuccess, (state, { connectorId }) => {
    const remainingResults = { ...state.testResults };
    delete remainingResults[connectorId];
    return {
      ...state,
      connectors: state.connectors.filter((item) => item.id !== connectorId),
      testResults: remainingResults,
      saving: false,
      error: null,
      successKey: 'connectors.feedback.deleted',
    };
  }),
  on(ConnectorsActions.deleteConnectorFailure, (state, { error }) => ({
    ...state,
    saving: false,
    error,
  })),

  on(ConnectorsActions.testConnector, (state, { connectorId }) => ({
    ...state,
    testingConnectorId: connectorId,
    error: null,
  })),
  on(ConnectorsActions.testConnectorSuccess, (state, { connectorId, result }) => ({
    ...state,
    testingConnectorId: null,
    testResults: {
      ...state.testResults,
      [connectorId]: result,
    },
    error: null,
  })),
  on(ConnectorsActions.testConnectorFailure, (state, { error }) => ({
    ...state,
    testingConnectorId: null,
    error,
  })),

  on(ConnectorsActions.loadDeadLetters, (state) => ({
    ...state,
    loadingDeadLetters: true,
    error: null,
  })),
  on(ConnectorsActions.loadDeadLettersSuccess, (state, { deadLetters }) => ({
    ...state,
    deadLetters,
    loadingDeadLetters: false,
    error: null,
  })),
  on(ConnectorsActions.loadDeadLettersFailure, (state, { error }) => ({
    ...state,
    loadingDeadLetters: false,
    error,
  })),

  on(ConnectorsActions.retryDeadLetter, (state, { outboxId }) => ({
    ...state,
    retryingOutboxId: outboxId,
    error: null,
    successKey: null,
  })),
  on(ConnectorsActions.retryDeadLetterSuccess, (state, { outboxId }) => ({
    ...state,
    deadLetters: state.deadLetters.filter((item) => item.id !== outboxId),
    retryingOutboxId: null,
    error: null,
    successKey: 'connectors.feedback.deadLetterRetried',
  })),
  on(ConnectorsActions.retryDeadLetterFailure, (state, { error }) => ({
    ...state,
    retryingOutboxId: null,
    error,
  })),

  on(ConnectorsActions.loadNeedsAssignmentTasks, (state) => ({
    ...state,
    loadingNeedsAssignment: true,
    error: null,
  })),
  on(ConnectorsActions.loadNeedsAssignmentTasksSuccess, (state, { tasks }) => ({
    ...state,
    needsAssignmentTasks: tasks,
    loadingNeedsAssignment: false,
    error: null,
  })),
  on(ConnectorsActions.loadNeedsAssignmentTasksFailure, (state, { error }) => ({
    ...state,
    loadingNeedsAssignment: false,
    error,
  })),

  on(ConnectorsActions.assignTask, (state, { taskId }) => ({
    ...state,
    assigningTaskId: taskId,
    error: null,
    successKey: null,
  })),
  on(ConnectorsActions.assignTaskSuccess, (state, { taskId }) => ({
    ...state,
    needsAssignmentTasks: state.needsAssignmentTasks.filter((task) => task.id !== taskId),
    assigningTaskId: null,
    error: null,
    successKey: 'connectors.feedback.taskAssigned',
  })),
  on(ConnectorsActions.assignTaskFailure, (state, { error }) => ({
    ...state,
    assigningTaskId: null,
    error,
  })),

  on(ConnectorsActions.clearFeedback, (state) => ({
    ...state,
    error: null,
    successKey: null,
  })),
);
