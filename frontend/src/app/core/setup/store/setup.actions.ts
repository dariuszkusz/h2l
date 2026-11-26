import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { components } from '../../api/generated/openapi.types';
import { MappedApiError } from '../../api/models/mapped-api-error.model';

export const SetupActions = createActionGroup({
  source: 'Setup',
  events: {
    'Check Status': emptyProps(),
    'Check Status Success': props<{ status: components['schemas']['SetupStatusResponse'] }>(),
    'Check Status Failure': props<{ error: MappedApiError }>(),
    Initialize: props<{ request: components['schemas']['InitializeSetupRequest'] }>(),
    'Initialize Success': props<{
      response: components['schemas']['InitializeSetupResponse'];
      language: components['schemas']['SupportedLanguage'];
      palette: components['schemas']['ColorPalette'];
    }>(),
    'Initialize Already Done': props<{ error: MappedApiError }>(),
    'Initialize Failure': props<{ error: MappedApiError }>(),
    'Clear Setup Error': emptyProps(),
    'Dismiss Already Initialized Notice': emptyProps(),
  },
});
