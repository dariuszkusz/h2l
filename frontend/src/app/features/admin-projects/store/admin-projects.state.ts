import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export interface AdminProjectsState {
  readonly projects: readonly components['schemas']['ProjectResponse'][];
  readonly selectedProjectId: string | null;
  readonly processes: readonly components['schemas']['WorkflowProcessResponse'][];
  readonly selectedProcessId: string | null;
  readonly forms: readonly components['schemas']['FormDefinitionResponse'][];
  readonly selectedFormId: string | null;
  readonly accessRules: readonly components['schemas']['FormAccessRuleItem'][];
  readonly loadingProjects: boolean;
  readonly loadingProcesses: boolean;
  readonly loadingForms: boolean;
  readonly loadingAccessRules: boolean;
  readonly saving: boolean;
  readonly error: MappedApiError | null;
  readonly successMessageKey: string | null;
}
