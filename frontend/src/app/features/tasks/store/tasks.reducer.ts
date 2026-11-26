import { createReducer, on } from '@ngrx/store';
import { AuthActions } from '../../../core/auth/store/auth.actions';
import { TasksActions } from './tasks.actions';
import { initialTasksState } from './tasks.state';

export const tasksReducer = createReducer(
  initialTasksState,

  on(TasksActions.setScope, (state, { scope }) => ({
    ...state,
    scope,
    page: 1,
  })),

  on(TasksActions.loadTasks, (state, { scope, page, pageSize }) => ({
    ...state,
    scope: scope ?? state.scope,
    page: page ?? state.page,
    pageSize: pageSize ?? state.pageSize,
    loadingList: true,
    error: null,
  })),
  on(TasksActions.loadTasksSuccess, (state, { response }) => ({
    ...state,
    loadingList: false,
    items: response.items,
    totalCount: response.totalCount,
    page: response.page,
    pageSize: response.pageSize,
  })),
  on(TasksActions.loadTasksFailure, (state, { error }) => ({
    ...state,
    loadingList: false,
    error,
  })),

  on(TasksActions.loadTaskDetail, (state) => ({
    ...state,
    loadingDetail: true,
    error: null,
    successKey: null,
    uploadedAttachments: [],
  })),
  on(TasksActions.loadTaskDetailSuccess, (state, { task, localDraftValues }) => ({
    ...state,
    loadingDetail: false,
    selectedTask: task,
    localDraftValues,
  })),
  on(TasksActions.loadTaskDetailFailure, (state, { error }) => ({
    ...state,
    loadingDetail: false,
    error,
  })),

  on(TasksActions.claimTask, (state) => ({
    ...state,
    mutating: true,
    error: null,
    successKey: null,
  })),
  on(TasksActions.claimTaskSuccess, (state, { task }) => ({
    ...state,
    mutating: false,
    selectedTask: state.selectedTask?.id === task.id ? task : state.selectedTask,
    items: state.items.map((item) =>
      item.id === task.id
        ? {
            ...item,
            status: task.status,
            assignedUserId: task.assignedUserId,
            claimedByUserId: task.claimedByUserId,
            canClaim: task.canClaim,
            canUnclaim: task.canUnclaim,
          }
        : item,
    ),
    successKey: 'tasks.feedback.claimed',
  })),
  on(TasksActions.claimTaskFailure, (state, { error }) => ({
    ...state,
    mutating: false,
    error,
  })),

  on(TasksActions.unclaimTask, (state) => ({
    ...state,
    mutating: true,
    error: null,
    successKey: null,
  })),
  on(TasksActions.unclaimTaskSuccess, (state, { task }) => ({
    ...state,
    mutating: false,
    selectedTask: state.selectedTask?.id === task.id ? task : state.selectedTask,
    localDraftValues: state.selectedTask?.id === task.id ? null : state.localDraftValues,
    queuedTaskIds: state.queuedTaskIds.filter((id) => id !== task.id),
    items: state.items.map((item) =>
      item.id === task.id
        ? {
            ...item,
            status: task.status,
            assignedUserId: task.assignedUserId,
            claimedByUserId: task.claimedByUserId,
            canClaim: task.canClaim,
            canUnclaim: task.canUnclaim,
          }
        : item,
    ),
    successKey: 'tasks.feedback.unclaimed',
  })),
  on(TasksActions.unclaimTaskFailure, (state, { error }) => ({
    ...state,
    mutating: false,
    error,
  })),

  on(TasksActions.uploadAttachment, (state) => ({
    ...state,
    uploadingAttachment: true,
    error: null,
  })),
  on(TasksActions.uploadAttachmentSuccess, (state, { attachment }) => ({
    ...state,
    uploadingAttachment: false,
    uploadedAttachments: [...state.uploadedAttachments, attachment],
  })),
  on(TasksActions.uploadAttachmentFailure, (state, { error }) => ({
    ...state,
    uploadingAttachment: false,
    error,
  })),

  on(TasksActions.submitTask, (state) => ({
    ...state,
    mutating: true,
    error: null,
    successKey: null,
  })),
  on(TasksActions.submitTaskSuccess, (state, { response }) => ({
    ...state,
    mutating: false,
    selectedTask:
      state.selectedTask?.id === response.taskId
        ? {
            ...state.selectedTask,
            status: response.status,
            completedAt: response.submittedAt,
            canClaim: false,
            canUnclaim: false,
          }
        : state.selectedTask,
    localDraftValues: state.selectedTask?.id === response.taskId ? null : state.localDraftValues,
    queuedTaskIds: state.queuedTaskIds.filter((id) => id !== response.taskId),
    items: state.items.map((item) =>
      item.id === response.taskId
        ? { ...item, status: response.status, canClaim: false, canUnclaim: false }
        : item,
    ),
    successKey: 'tasks.feedback.submitted',
  })),
  on(TasksActions.submitTaskQueuedOffline, (state, { taskId }) => ({
    ...state,
    mutating: false,
    queuedTaskIds: state.queuedTaskIds.includes(taskId)
      ? state.queuedTaskIds
      : [...state.queuedTaskIds, taskId],
    successKey: 'tasks.feedback.queuedOffline',
  })),
  on(TasksActions.submitTaskFailure, (state, { error }) => ({
    ...state,
    mutating: false,
    error,
  })),

  on(TasksActions.queuedSubmissionsLoaded, (state, { queuedTaskIds }) => ({
    ...state,
    queuedTaskIds,
  })),
  on(TasksActions.syncQueuedSubmissionCompleted, (state, { taskId }) => ({
    ...state,
    queuedTaskIds: state.queuedTaskIds.filter((id) => id !== taskId),
    items: state.items.map((item) =>
      item.id === taskId
        ? { ...item, status: 'Completed', canClaim: false, canUnclaim: false }
        : item,
    ),
    successKey: 'tasks.feedback.syncedOffline',
  })),

  on(TasksActions.clearFeedback, (state) => ({
    ...state,
    error: null,
    successKey: null,
  })),
  on(AuthActions.logoutCompleted, AuthActions.sessionExpired, () => initialTasksState),
);
