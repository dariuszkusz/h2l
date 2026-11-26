import { components } from '../../api/generated/openapi.types';
import { MappedApiError } from '../../api/models/mapped-api-error.model';
import { ActivationStatus } from '../models/activation-status.type';

export interface AuthState {
  readonly user: components['schemas']['UserProfileResponse'] | null;
  readonly accessToken: string | null;
  readonly refreshToken: string | null;
  readonly isAuthenticated: boolean;
  readonly hydrated: boolean;
  readonly loading: boolean;
  readonly error: MappedApiError | null;
  readonly sessionExpiredNotice: boolean;
  readonly forbiddenError: MappedApiError | null;
  readonly activationStatus: ActivationStatus;
  readonly activationError: MappedApiError | null;
  readonly preferencesSaving: boolean;
  readonly preferencesSaved: boolean;
  readonly preferencesError: MappedApiError | null;
}
