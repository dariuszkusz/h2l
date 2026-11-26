import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export const ConnectorsActions = createActionGroup({
  source: 'Connectors',
  events: {
    'Load Connectors': emptyProps(),
    'Load Connectors Success': props<{
      readonly connectors: readonly components['schemas']['ConnectorSummaryResponse'][];
    }>(),
    'Load Connectors Failure': props<{ readonly error: MappedApiError }>(),

    'Create Connector': props<{
      readonly request: components['schemas']['CreateConnectorRequest'];
    }>(),
    'Create Connector Success': props<{
      readonly connector: components['schemas']['ConnectorSummaryResponse'];
    }>(),
    'Create Connector Failure': props<{ readonly error: MappedApiError }>(),

    'Update Connector': props<{
      readonly connectorId: string;
      readonly request: components['schemas']['UpdateConnectorRequest'];
    }>(),
    'Update Connector Success': props<{
      readonly connector: components['schemas']['ConnectorSummaryResponse'];
    }>(),
    'Update Connector Failure': props<{ readonly error: MappedApiError }>(),

    'Delete Connector': props<{ readonly connectorId: string }>(),
    'Delete Connector Success': props<{ readonly connectorId: string }>(),
    'Delete Connector Failure': props<{ readonly error: MappedApiError }>(),

    'Test Connector': props<{ readonly connectorId: string }>(),
    'Test Connector Success': props<{
      readonly connectorId: string;
      readonly result: components['schemas']['TestConnectorConnectionResponse'];
    }>(),
    'Test Connector Failure': props<{
      readonly connectorId: string;
      readonly error: MappedApiError;
    }>(),

    'Load Dead Letters': emptyProps(),
    'Load Dead Letters Success': props<{
      readonly deadLetters: readonly components['schemas']['DeadLetterItemResponse'][];
    }>(),
    'Load Dead Letters Failure': props<{ readonly error: MappedApiError }>(),

    'Retry Dead Letter': props<{ readonly outboxId: string }>(),
    'Retry Dead Letter Success': props<{ readonly outboxId: string }>(),
    'Retry Dead Letter Failure': props<{ readonly error: MappedApiError }>(),

    'Load Needs Assignment Tasks': emptyProps(),
    'Load Needs Assignment Tasks Success': props<{
      readonly tasks: readonly components['schemas']['TaskSummaryResponse'][];
    }>(),
    'Load Needs Assignment Tasks Failure': props<{ readonly error: MappedApiError }>(),

    'Assign Task': props<{
      readonly taskId: string;
      readonly userId: string;
    }>(),
    'Assign Task Success': props<{ readonly taskId: string }>(),
    'Assign Task Failure': props<{ readonly error: MappedApiError }>(),

    'Clear Feedback': emptyProps(),
  },
});
