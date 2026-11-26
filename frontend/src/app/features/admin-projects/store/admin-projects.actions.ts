import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export const AdminProjectsActions = createActionGroup({
  source: 'AdminProjects',
  events: {
    'Load Projects': emptyProps(),
    'Load Projects Success': props<{ projects: components['schemas']['ProjectResponse'][] }>(),
    'Load Projects Failure': props<{ error: MappedApiError }>(),

    'Create Project': props<{ request: components['schemas']['CreateProjectRequest'] }>(),
    'Create Project Success': props<{ project: components['schemas']['ProjectResponse'] }>(),
    'Create Project Failure': props<{ error: MappedApiError }>(),

    'Select Project': props<{ projectId: string | null }>(),
    'Load Processes': props<{ projectId: string }>(),
    'Load Processes Success': props<{
      projectId: string;
      processes: components['schemas']['WorkflowProcessResponse'][];
    }>(),
    'Load Processes Failure': props<{ error: MappedApiError }>(),

    'Create Process': props<{
      projectId: string;
      request: components['schemas']['CreateWorkflowProcessRequest'];
    }>(),
    'Create Process Success': props<{
      process: components['schemas']['WorkflowProcessResponse'];
    }>(),
    'Create Process Failure': props<{ error: MappedApiError }>(),

    'Select Process': props<{ processId: string | null }>(),
    'Load Forms': props<{ processId: string }>(),
    'Load Forms Success': props<{
      processId: string;
      forms: components['schemas']['FormDefinitionResponse'][];
    }>(),
    'Load Forms Failure': props<{ error: MappedApiError }>(),

    'Create Form': props<{
      processId: string;
      request: components['schemas']['CreateFormDefinitionRequest'];
    }>(),
    'Create Form Success': props<{ form: components['schemas']['FormDefinitionResponse'] }>(),
    'Create Form Failure': props<{ error: MappedApiError }>(),

    'Select Form': props<{ formId: string | null }>(),
    'Load Access Rules': props<{ formId: string }>(),
    'Load Access Rules Success': props<{
      formId: string;
      rules: components['schemas']['FormAccessRuleItem'][];
    }>(),
    'Load Access Rules Failure': props<{ error: MappedApiError }>(),

    'Save Access Rules': props<{
      formId: string;
      request: components['schemas']['UpdateFormAccessRulesRequest'];
    }>(),
    'Save Access Rules Success': props<{ formId: string }>(),
    'Save Access Rules Failure': props<{ error: MappedApiError }>(),

    'Clear Feedback': emptyProps(),
  },
});
