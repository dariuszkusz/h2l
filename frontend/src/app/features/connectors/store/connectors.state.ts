import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export interface ConnectorsState {
  readonly connectors: readonly components['schemas']['ConnectorSummaryResponse'][];
  readonly deadLetters: readonly components['schemas']['DeadLetterItemResponse'][];
  readonly needsAssignmentTasks: readonly components['schemas']['TaskSummaryResponse'][];
  readonly loadingConnectors: boolean;
  readonly loadingDeadLetters: boolean;
  readonly loadingNeedsAssignment: boolean;
  readonly saving: boolean;
  readonly testingConnectorId: string | null;
  readonly testResults: Readonly<
    Record<string, components['schemas']['TestConnectorConnectionResponse']>
  >;
  readonly retryingOutboxId: string | null;
  readonly assigningTaskId: string | null;
  readonly error: MappedApiError | null;
  readonly successKey: string | null;
}

export const initialConnectorsState: ConnectorsState = {
  connectors: [],
  deadLetters: [],
  needsAssignmentTasks: [],
  loadingConnectors: false,
  loadingDeadLetters: false,
  loadingNeedsAssignment: false,
  saving: false,
  testingConnectorId: null,
  testResults: {},
  retryingOutboxId: null,
  assigningTaskId: null,
  error: null,
  successKey: null,
};
