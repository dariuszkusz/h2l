import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export interface TasksState {
  readonly items: readonly components['schemas']['TaskSummaryResponse'][];
  readonly totalCount: number;
  readonly page: number;
  readonly pageSize: number;
  readonly scope: components['schemas']['TaskListScope'];
  readonly selectedTask: components['schemas']['TaskDetailResponse'] | null;
  readonly localDraftValues: Readonly<Record<string, unknown>> | null;
  readonly queuedTaskIds: readonly string[];
  readonly uploadedAttachments: readonly components['schemas']['AttachmentUploadResponse'][];
  readonly loadingList: boolean;
  readonly loadingDetail: boolean;
  readonly mutating: boolean;
  readonly uploadingAttachment: boolean;
  readonly error: MappedApiError | null;
  readonly successKey: string | null;
}

export const initialTasksState: TasksState = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
  scope: 'all',
  selectedTask: null,
  localDraftValues: null,
  queuedTaskIds: [],
  uploadedAttachments: [],
  loadingList: false,
  loadingDetail: false,
  mutating: false,
  uploadingAttachment: false,
  error: null,
  successKey: null,
};
