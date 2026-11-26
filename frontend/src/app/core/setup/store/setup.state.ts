import { MappedApiError } from '../../api/models/mapped-api-error.model';

export interface SetupState {
  readonly isInitialized: boolean | null;
  readonly instanceId: string | null;
  readonly loadingStatus: boolean;
  readonly submitting: boolean;
  readonly error: MappedApiError | null;
  readonly initializedSuccess: boolean;
  readonly alreadyInitializedRedirect: boolean;
}
