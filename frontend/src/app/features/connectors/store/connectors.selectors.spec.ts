import { describe, expect, it } from 'vitest';
import {
  selectActiveConnectorsCount,
  selectAssigningTaskId,
  selectConnectorsError,
  selectConnectorsList,
  selectConnectorsLoading,
  selectConnectorsSaving,
  selectConnectorsSuccessKey,
  selectConnectorTestResults,
  selectDeadLettersList,
  selectDeadLettersLoading,
  selectNeedsAssignmentLoading,
  selectNeedsAssignmentTasks,
  selectRetryingOutboxId,
  selectTestingConnectorId,
} from './connectors.selectors';
import { ConnectorsState } from './connectors.state';

describe('connectors selectors', () => {
  const state: ConnectorsState = {
    connectors: [
      {
        id: 'c-1',
        name: 'Camunda 8',
        connectorType: 'Camunda8',
        baseUrl: 'https://camunda.local',
        authType: 'None',
        hasCredentials: false,
        hasCustomCaCertificate: false,
        callbackUrl: null,
        isActive: true,
        createdAt: '2026-11-26T00:00:00Z',
      },
      {
        id: 'c-2',
        name: 'n8n Inactive',
        connectorType: 'N8n',
        baseUrl: 'https://n8n.local',
        authType: 'HmacSha256',
        hasCredentials: true,
        hasCustomCaCertificate: true,
        callbackUrl: null,
        isActive: false,
        createdAt: '2026-11-26T00:00:00Z',
      },
    ],
    deadLetters: [
      {
        id: 'dl-1',
        humanTaskId: 't-1',
        formSubmissionId: 's-1',
        connectorName: 'n8n',
        messageType: 'CompleteTask',
        retryCount: 4,
        deadLetterReason: 'HTTP 400 Bad Request',
        createdAt: '2026-11-26T00:00:00Z',
      },
    ],
    needsAssignmentTasks: [],
    loadingConnectors: false,
    loadingDeadLetters: true,
    loadingNeedsAssignment: false,
    saving: false,
    testingConnectorId: 'c-1',
    testResults: {
      'c-1': {
        isReachable: true,
        detectedEngine: 'Camunda 8',
        detectedVersion: '8.7.0',
        latencyMs: 18,
        errorMessage: null,
      },
    },
    retryingOutboxId: 'dl-1',
    assigningTaskId: null,
    error: null,
    successKey: 'connectors.feedback.created',
  };

  it('selects connectors, active count, dead-letters, and operational flags', () => {
    expect(selectConnectorsList.projector(state)).toHaveLength(2);
    expect(selectActiveConnectorsCount.projector(state.connectors)).toBe(1);
    expect(selectDeadLettersList.projector(state)).toHaveLength(1);
    expect(selectNeedsAssignmentTasks.projector(state)).toHaveLength(0);
    expect(selectConnectorsLoading.projector(state)).toBe(false);
    expect(selectDeadLettersLoading.projector(state)).toBe(true);
    expect(selectNeedsAssignmentLoading.projector(state)).toBe(false);
    expect(selectConnectorsSaving.projector(state)).toBe(false);
    expect(selectTestingConnectorId.projector(state)).toBe('c-1');
    expect(selectConnectorTestResults.projector(state)['c-1']?.latencyMs).toBe(18);
    expect(selectRetryingOutboxId.projector(state)).toBe('dl-1');
    expect(selectAssigningTaskId.projector(state)).toBeNull();
    expect(selectConnectorsError.projector(state)).toBeNull();
    expect(selectConnectorsSuccessKey.projector(state)).toBe('connectors.feedback.created');
  });
});
