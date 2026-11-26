import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export interface FormBuilderState {
  readonly formDefinition: components['schemas']['FormDefinitionDetailResponse'] | null;
  readonly draftName: string;
  readonly draftSchema: components['schemas']['FormSchema'] | null;
  readonly versions: readonly components['schemas']['FormVersionSummaryResponse'][];
  readonly selectedStepIndex: number;
  readonly selectedComponentId: string | null;
  readonly dirty: boolean;
  readonly loading: boolean;
  readonly loadingVersions: boolean;
  readonly saving: boolean;
  readonly publishing: boolean;
  readonly error: MappedApiError | null;
  readonly successKey: string | null;
}

export const initialFormBuilderState: FormBuilderState = {
  formDefinition: null,
  draftName: '',
  draftSchema: null,
  versions: [],
  selectedStepIndex: 0,
  selectedComponentId: null,
  dirty: false,
  loading: false,
  loadingVersions: false,
  saving: false,
  publishing: false,
  error: null,
  successKey: null,
};
