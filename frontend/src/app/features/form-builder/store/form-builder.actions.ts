import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export const FormBuilderActions = createActionGroup({
  source: 'Form Builder',
  events: {
    'Load Form': props<{ formId: string }>(),
    'Load Form Success': props<{
      form: components['schemas']['FormDefinitionDetailResponse'];
    }>(),
    'Load Form Failure': props<{ error: MappedApiError }>(),

    'Load Versions': props<{ formId: string }>(),
    'Load Versions Success': props<{
      versions: readonly components['schemas']['FormVersionSummaryResponse'][];
    }>(),
    'Load Versions Failure': props<{ error: MappedApiError }>(),

    'Update Draft Name': props<{ name: string }>(),
    'Update Draft Schema': props<{ schema: components['schemas']['FormSchema'] }>(),
    'Select Step': props<{ stepIndex: number }>(),
    'Select Component': props<{ componentId: string | null }>(),

    'Save Draft': props<{
      formId: string;
      request: components['schemas']['UpdateFormDraftRequest'];
    }>(),
    'Save Draft Success': props<{
      form: components['schemas']['FormDefinitionDetailResponse'];
    }>(),
    'Save Draft Failure': props<{ error: MappedApiError }>(),

    'Publish Version': props<{ formId: string }>(),
    'Publish Version Success': props<{
      version: components['schemas']['FormVersionResponse'];
    }>(),
    'Publish Version Failure': props<{ error: MappedApiError }>(),

    'Clear Feedback': emptyProps(),
    Reset: emptyProps(),
  },
});
