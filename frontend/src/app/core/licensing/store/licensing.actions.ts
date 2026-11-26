import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { components } from '../../api/generated/openapi.types';
import { MappedApiError } from '../../api/models/mapped-api-error.model';

export const LicensingActions = createActionGroup({
  source: 'Licensing',
  events: {
    'Load Status': emptyProps(),
    'Load Status Success': props<{ status: components['schemas']['LicensingStatusResponse'] }>(),
    'Load Status Failure': props<{ error: MappedApiError }>(),
    'Activate License': props<{ request: components['schemas']['ActivateLicenseRequest'] }>(),
    'Activate License Success': props<{
      status: components['schemas']['LicensingStatusResponse'];
    }>(),
    'Activate License Failure': props<{ error: MappedApiError }>(),
    'Clear Feedback': emptyProps(),
  },
});
