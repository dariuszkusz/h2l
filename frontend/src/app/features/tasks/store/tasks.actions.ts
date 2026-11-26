import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { MappedApiError } from '../../../core/api/models/mapped-api-error.model';

export const TasksActions = createActionGroup({
  source: 'Tasks',
  events: {
    'Set Scope': props<{ scope: components['schemas']['TaskListScope'] }>(),

    'Load Tasks': props<{
      scope?: components['schemas']['TaskListScope'];
      page?: number;
      pageSize?: number;
      preserveError?: boolean;
    }>(),
    'Load Tasks Success': props<{
      response: components['schemas']['TaskListResponse'];
    }>(),
    'Load Tasks Failure': props<{ error: MappedApiError }>(),

    'Load Task Detail': props<{ taskId: string }>(),
    'Load Task Detail Success': props<{
      task: components['schemas']['TaskDetailResponse'];
      localDraftValues: Readonly<Record<string, unknown>> | null;
    }>(),
    'Load Task Detail Failure': props<{ error: MappedApiError }>(),

    'Claim Task': props<{ taskId: string }>(),
    'Claim Task Success': props<{
      task: components['schemas']['TaskDetailResponse'];
    }>(),
    'Claim Task Failure': props<{ error: MappedApiError; taskId: string }>(),

    'Unclaim Task': props<{ taskId: string }>(),
    'Unclaim Task Success': props<{
      task: components['schemas']['TaskDetailResponse'];
    }>(),
    'Unclaim Task Failure': props<{ error: MappedApiError }>(),

    'Save Local Draft': props<{
      taskId: string;
      values: Readonly<Record<string, unknown>>;
    }>(),

    'Upload Attachment': props<{
      taskId: string;
      fieldVariableKey: string;
      file: File;
    }>(),
    'Upload Attachment Success': props<{
      attachment: components['schemas']['AttachmentUploadResponse'];
    }>(),
    'Upload Attachment Failure': props<{ error: MappedApiError }>(),

    'Submit Task': props<{
      taskId: string;
      values: Readonly<Record<string, unknown>>;
      attachmentIds: readonly string[];
      idempotencyKey?: string;
    }>(),
    'Submit Task Success': props<{
      response: components['schemas']['SubmitTaskResponse'];
    }>(),
    'Submit Task Queued Offline': props<{
      taskId: string;
      idempotencyKey: string;
    }>(),
    'Submit Task Failure': props<{ error: MappedApiError }>(),

    'Load Queued Submissions': emptyProps(),
    'Queued Submissions Loaded': props<{ queuedTaskIds: readonly string[] }>(),
    'Sync Queued Submissions': emptyProps(),
    'Sync Queued Submission Completed': props<{ taskId: string }>(),

    'Realtime Event Received': props<{ eventName: string; taskId: string | null }>(),
    'Clear Feedback': emptyProps(),
  },
});
