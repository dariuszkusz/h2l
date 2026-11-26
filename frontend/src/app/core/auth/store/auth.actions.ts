import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { components } from '../../api/generated/openapi.types';
import { MappedApiError } from '../../api/models/mapped-api-error.model';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    Login: props<{
      request: components['schemas']['LoginRequest'];
      returnUrl?: string | null;
    }>(),
    'Login Success': props<{
      response: components['schemas']['AuthTokenResponse'];
      returnUrl?: string | null;
    }>(),
    'Login Failure': props<{ error: MappedApiError }>(),
    'Refresh Token': emptyProps(),
    'Refresh Token Success': props<{ response: components['schemas']['AuthTokenResponse'] }>(),
    'Refresh Token Failure': props<{ error: MappedApiError }>(),
    Logout: emptyProps(),
    'Logout Completed': emptyProps(),
    'Hydrate Session': emptyProps(),
    'Hydrate Session Success': props<{
      user: components['schemas']['UserProfileResponse'];
      accessToken: string;
      refreshToken: string;
    }>(),
    'Hydrate Session Skipped': emptyProps(),
    'Hydrate Session Failure': props<{ error: MappedApiError }>(),
    'Session Expired': emptyProps(),
    'Forbidden Encountered': props<{ error: MappedApiError }>(),
    'Clear Auth Error': emptyProps(),
    'Activate Account': props<{ request: components['schemas']['ActivateAccountRequest'] }>(),
    'Activate Account Success': emptyProps(),
    'Activate Account Failure': props<{ error: MappedApiError }>(),
    'Reset Activation State': emptyProps(),
    'Update Preferences': props<{
      request: components['schemas']['UpdateUserPreferencesRequest'];
    }>(),
    'Update Preferences Success': props<{
      request: components['schemas']['UpdateUserPreferencesRequest'];
    }>(),
    'Update Preferences Failure': props<{ error: MappedApiError }>(),
    'Clear Preferences Feedback': emptyProps(),
  },
});
