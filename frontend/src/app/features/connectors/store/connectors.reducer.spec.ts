import { describe, expect, it } from 'vitest';
import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';
import { ConnectorsActions } from './connectors.actions';
import { connectorsReducer } from './connectors.reducer';
import { initialConnectorsState } from './connectors.state';

const sampleConnector: components['schemas']['ConnectorSummaryResponse'] = {
  id: 'conn-1',
  name: 'Camunda Prod',
  connectorType: 'Camunda8',
  baseUrl: 'https://camunda.internal:8080',
  authType: 'OidcClientCredentials',
  hasCredentials: true,
  hasCustomCaCertificate: true,
  callbackUrl: null,
  isActive: true,
  createdAt: '2026-11-26T08:00:00Z',
};

const sampleDeadLetter: components['schemas']['DeadLetterItemResponse'] = {
  id: 'outbox-1',
  humanTaskId: 'task-1',
  formSubmissionId: 'sub-1',
  connectorName: 'n8n Cloud',
  messageType: 'CompleteTaskCallback',
  retryCount: 5,
  deadLetterReason: 'HTTP 404 Not Found',
  createdAt: '2026-11-26T08:30:00Z',
};

const sampleTask: components['schemas']['TaskSummaryResponse'] = {
  id: 'task-na-1',
  formDefinitionId: 'form-1',
  formVersionNumber: 1,
  formName: 'Software Request',
  processName: 'Procurement',
  projectName: 'IT',
  status: 'NeedsAssignment',
  assignedUserId: null,
  claimedByUserId: null,
  canClaim: false,
  canUnclaim: false,
  expiresAt: null,
  createdAt: '2026-11-26T08:40:00Z',
};

const planLimitError: MappedApiError = {
  status: 402,
  errorCode: 'FREE_PLAN_CONNECTOR_LIMIT_EXCEEDED',
  messageKey: 'errors.codes.PLAN_LIMIT_EXCEEDED',
  detail: 'Free plan allows 1 active connector.',
  fieldErrors: {},
  isOffline: false,
};

describe('connectorsReducer', () => {
  it('loads, creates, updates, tests, and deletes connectors', () => {
    let state = connectorsReducer(initialConnectorsState, ConnectorsActions.loadConnectors());
    expect(state.loadingConnectors).toBe(true);

    state = connectorsReducer(
      state,
      ConnectorsActions.loadConnectorsSuccess({ connectors: [sampleConnector] }),
    );
    expect(state.loadingConnectors).toBe(false);
    expect(state.connectors).toHaveLength(1);

    const created: components['schemas']['ConnectorSummaryResponse'] = {
      ...sampleConnector,
      id: 'conn-2',
      name: 'n8n Self-Hosted',
      connectorType: 'N8n',
    };
    state = connectorsReducer(state, ConnectorsActions.createConnectorSuccess({ connector: created }));
    expect(state.connectors).toHaveLength(2);
    expect(state.successKey).toBe('connectors.feedback.created');

    state = connectorsReducer(
      state,
      ConnectorsActions.updateConnectorSuccess({
        connector: { ...created, name: 'n8n Updated' },
      }),
    );
    expect(state.connectors.find((c) => c.id === 'conn-2')?.name).toBe('n8n Updated');
    expect(state.successKey).toBe('connectors.feedback.updated');

    state = connectorsReducer(
      state,
      ConnectorsActions.testConnectorSuccess({
        connectorId: 'conn-2',
        result: {
          isReachable: true,
          detectedEngine: 'n8n',
          detectedVersion: '1.78.0',
          latencyMs: 35,
          errorMessage: null,
        },
      }),
    );
    expect(state.testResults['conn-2']?.isReachable).toBe(true);

    state = connectorsReducer(state, ConnectorsActions.deleteConnectorSuccess({ connectorId: 'conn-2' }));
    expect(state.connectors).toHaveLength(1);
    expect(state.testResults['conn-2']).toBeUndefined();
    expect(state.successKey).toBe('connectors.feedback.deleted');
  });

  it('stores 402 plan limit error on createConnectorFailure', () => {
    const state = connectorsReducer(
      initialConnectorsState,
      ConnectorsActions.createConnectorFailure({ error: planLimitError }),
    );
    expect(state.saving).toBe(false);
    expect(state.error?.status).toBe(402);
  });

  it('handles dead-letter list and retry removal', () => {
    let state = connectorsReducer(
      initialConnectorsState,
      ConnectorsActions.loadDeadLettersSuccess({ deadLetters: [sampleDeadLetter] }),
    );
    expect(state.deadLetters).toHaveLength(1);

    state = connectorsReducer(state, ConnectorsActions.retryDeadLetter({ outboxId: 'outbox-1' }));
    expect(state.retryingOutboxId).toBe('outbox-1');

    state = connectorsReducer(
      state,
      ConnectorsActions.retryDeadLetterSuccess({ outboxId: 'outbox-1' }),
    );
    expect(state.deadLetters).toHaveLength(0);
    expect(state.retryingOutboxId).toBeNull();
    expect(state.successKey).toBe('connectors.feedback.deadLetterRetried');
  });

  it('handles NeedsAssignment list and assignment removal', () => {
    let state = connectorsReducer(
      initialConnectorsState,
      ConnectorsActions.loadNeedsAssignmentTasksSuccess({ tasks: [sampleTask] }),
    );
    expect(state.needsAssignmentTasks).toHaveLength(1);

    state = connectorsReducer(
      state,
      ConnectorsActions.assignTask({ taskId: 'task-na-1', userId: 'u-1' }),
    );
    expect(state.assigningTaskId).toBe('task-na-1');

    state = connectorsReducer(
      state,
      ConnectorsActions.assignTaskSuccess({ taskId: 'task-na-1' }),
    );
    expect(state.needsAssignmentTasks).toHaveLength(0);
    expect(state.assigningTaskId).toBeNull();
    expect(state.successKey).toBe('connectors.feedback.taskAssigned');
  });
});
