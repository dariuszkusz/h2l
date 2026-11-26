import { components } from '../../api/generated/openapi.types';
import { MappedApiError } from '../../api/models/mapped-api-error.model';

export interface LicensingState {
  readonly status: components['schemas']['LicensingStatusResponse'] | null;
  readonly loading: boolean;
  readonly activating: boolean;
  readonly error: MappedApiError | null;
  readonly activationSuccess: boolean;
}
