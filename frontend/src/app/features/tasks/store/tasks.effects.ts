import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import {
  catchError,
  concatMap,
  debounceTime,
  exhaustMap,
  filter,
  from,
  map,
  mergeMap,
  of,
  switchMap,
  tap,
  withLatestFrom,
} from 'rxjs';
import { ProblemDetailsMapperService } from '../../../core/api/services/problem-details-mapper.service';
import { TasksApiService } from '../../../core/api/tasks-api.service';
import { AuthActions } from '../../../core/auth/store/auth.actions';
import { OfflineDraftStorageService } from '../../../core/connectivity/services/offline-draft-storage.service';
import { ConnectivityActions } from '../../../core/connectivity/store/connectivity.actions';
import { selectIsOnline } from '../../../core/connectivity/store/connectivity.selectors';
import { TasksSignalRService } from '../../../core/realtime/tasks-signalr.service';
import { TasksActions } from './tasks.actions';
import { selectSelectedTask, selectTasksScope } from './tasks.selectors';

function generateIdempotencyKey(): string {
  if (typeof globalThis.crypto !== 'undefined' && typeof globalThis.crypto.randomUUID === 'function') {
    return globalThis.crypto.randomUUID();
  }
  return `h2l-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

@Injectable()
export class TasksEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store);
  private readonly tasksApi = inject(TasksApiService);
  private readonly draftStorage = inject(OfflineDraftStorageService);
  private readonly problemDetailsMapper = inject(ProblemDetailsMapperService);
  private readonly signalR = inject(TasksSignalRService);

  readonly connectSignalROnAuth$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(
          AuthActions.loginSuccess,
          AuthActions.hydrateSessionSuccess,
          AuthActions.refreshTokenSuccess,
        ),
        tap((action) => {
          const accessToken =
            'response' in action ? action.response.accessToken : action.accessToken;
          this.signalR.connect(accessToken);
        }),
      ),
    { dispatch: false },
  );

  readonly disconnectSignalROnLogout$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.logout, AuthActions.logoutCompleted, AuthActions.sessionExpired),
        tap(() => {
          this.signalR.disconnect();
        }),
      ),
    { dispatch: false },
  );

  readonly listenSignalREvents$ = createEffect(() =>
    this.signalR.taskEvents$.pipe(
      map((event) =>
        TasksActions.realtimeEventReceived({
          eventName: event.eventName,
          taskId: event.taskId,
        }),
      ),
    ),
  );

  readonly refreshOnRealtimeEvent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.realtimeEventReceived),
      withLatestFrom(this.store.select(selectTasksScope)),
      map(([, scope]) => TasksActions.loadTasks({ scope })),
    ),
  );

  readonly loadTasks$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.loadTasks, TasksActions.setScope),
      withLatestFrom(this.store.select(selectTasksScope)),
      switchMap(([action, currentScope]) => {
        const scope = action.scope ?? currentScope;
        const page = 'page' in action ? action.page : undefined;
        const pageSize = 'pageSize' in action ? action.pageSize : undefined;
        return this.tasksApi.listTasks({ scope, page, pageSize }).pipe(
          map((response) => TasksActions.loadTasksSuccess({ response })),
          catchError((error: unknown) =>
            of(TasksActions.loadTasksFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        );
      }),
    ),
  );

  readonly loadTaskDetail$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.loadTaskDetail),
      switchMap(({ taskId }) =>
        this.tasksApi.getTaskDetail(taskId).pipe(
          switchMap((task) =>
            from(this.draftStorage.getDraft(taskId)).pipe(
              catchError(() => of(null)),
              map((draft) =>
                TasksActions.loadTaskDetailSuccess({
                  task,
                  localDraftValues: draft?.payload ?? null,
                }),
              ),
            ),
          ),
          catchError((error: unknown) =>
            of(TasksActions.loadTaskDetailFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        ),
      ),
    ),
  );

  readonly claimTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.claimTask),
      exhaustMap(({ taskId }) =>
        this.tasksApi.claimTask(taskId).pipe(
          map((task) => TasksActions.claimTaskSuccess({ task })),
          catchError((error: unknown) =>
            of(
              TasksActions.claimTaskFailure({
                error: this.problemDetailsMapper.map(error),
                taskId,
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly refreshListOnClaimConflict$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.claimTaskFailure),
      filter(({ error }) => error.status === 409 || error.errorCode === 'TASK_ALREADY_CLAIMED'),
      withLatestFrom(this.store.select(selectTasksScope)),
      map(([, scope]) => TasksActions.loadTasks({ scope })),
    ),
  );

  readonly unclaimTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.unclaimTask),
      exhaustMap(({ taskId }) =>
        this.tasksApi.unclaimTask(taskId).pipe(
          switchMap((task) =>
            from(this.draftStorage.deleteDraft(taskId)).pipe(
              catchError(() => of(void 0)),
              switchMap(() => this.refreshPendingCountActions()),
              mergeMap((connectivityAction) => [
                TasksActions.unclaimTaskSuccess({ task }),
                connectivityAction,
              ]),
            ),
          ),
          catchError((error: unknown) =>
            of(TasksActions.unclaimTaskFailure({ error: this.problemDetailsMapper.map(error) })),
          ),
        ),
      ),
    ),
  );

  readonly saveLocalDraft$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(TasksActions.saveLocalDraft),
        debounceTime(250),
        concatMap(({ taskId, values }) =>
          from(this.draftStorage.saveDraft(taskId, values)).pipe(catchError(() => of(void 0))),
        ),
      ),
    { dispatch: false },
  );

  readonly uploadAttachment$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.uploadAttachment),
      mergeMap(({ taskId, fieldVariableKey, file }) =>
        this.tasksApi.uploadTaskAttachment(taskId, fieldVariableKey, file).pipe(
          map((attachment) => TasksActions.uploadAttachmentSuccess({ attachment })),
          catchError((error: unknown) =>
            of(
              TasksActions.uploadAttachmentFailure({
                error: this.problemDetailsMapper.map(error),
              }),
            ),
          ),
        ),
      ),
    ),
  );

  readonly submitTask$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.submitTask),
      withLatestFrom(this.store.select(selectIsOnline)),
      exhaustMap(([{ taskId, values, attachmentIds, idempotencyKey }, isOnline]) => {
        const key = idempotencyKey ?? generateIdempotencyKey();

        if (!isOnline) {
          return this.queueSubmissionOffline(taskId, values, attachmentIds, key);
        }

        return this.tasksApi
          .submitTask(taskId, { values, attachmentIds: [...attachmentIds] }, key)
          .pipe(
            switchMap((response) =>
              from(this.draftStorage.deleteDraft(taskId)).pipe(
                catchError(() => of(void 0)),
                switchMap(() => this.refreshPendingCountActions()),
                mergeMap((connectivityAction) => [
                  TasksActions.submitTaskSuccess({ response }),
                  connectivityAction,
                ]),
              ),
            ),
            catchError((error: unknown) => {
              const mapped = this.problemDetailsMapper.map(error);
              if (mapped.isOffline) {
                return this.queueSubmissionOffline(taskId, values, attachmentIds, key);
              }
              return of(TasksActions.submitTaskFailure({ error: mapped }));
            }),
          );
      }),
    ),
  );

  readonly loadQueuedSubmissions$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.loadQueuedSubmissions, ConnectivityActions.initialize),
      switchMap(() =>
        from(this.draftStorage.listQueuedSubmissions()).pipe(
          catchError(() => of([])),
          mergeMap((queued) => [
            TasksActions.queuedSubmissionsLoaded({
              queuedTaskIds: queued.map((q) => q.taskId),
            }),
            ConnectivityActions.pendingDraftsUpdated({
              pendingDraftsCount: queued.length,
            }),
          ]),
        ),
      ),
    ),
  );

  readonly triggerSyncOnReconnect$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ConnectivityActions.statusChanged),
      filter(({ isOnline }) => isOnline),
      map(() => TasksActions.syncQueuedSubmissions()),
    ),
  );

  readonly syncQueuedSubmissions$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.syncQueuedSubmissions),
      switchMap(() =>
        from(this.draftStorage.listQueuedSubmissions()).pipe(
          catchError(() => of([])),
          switchMap((queued) => {
            if (queued.length === 0) {
              return of(
                ConnectivityActions.pendingDraftsUpdated({ pendingDraftsCount: 0 }),
                ConnectivityActions.syncStatusChanged({ syncStatus: 'idle' }),
              );
            }

            return from(queued).pipe(
              concatMap((record) => {
                const key = record.idempotencyKey ?? generateIdempotencyKey();
                return this.tasksApi
                  .submitTask(
                    record.taskId,
                    {
                      values: record.payload,
                      attachmentIds: [...(record.attachmentIds ?? [])],
                    },
                    key,
                  )
                  .pipe(
                    switchMap(() =>
                      from(this.draftStorage.deleteDraft(record.taskId)).pipe(
                        catchError(() => of(void 0)),
                        switchMap(() => this.refreshPendingCountActions()),
                        mergeMap((pendingAction) => [
                          TasksActions.syncQueuedSubmissionCompleted({ taskId: record.taskId }),
                          pendingAction,
                          ConnectivityActions.syncStatusChanged({ syncStatus: 'idle' }),
                        ]),
                      ),
                    ),
                    catchError(() =>
                      of(ConnectivityActions.syncStatusChanged({ syncStatus: 'error' })),
                    ),
                  );
              }),
            );
          }),
        ),
      ),
    ),
  );

  readonly refreshSelectedTaskOnSync$ = createEffect(() =>
    this.actions$.pipe(
      ofType(TasksActions.syncQueuedSubmissionCompleted),
      withLatestFrom(this.store.select(selectSelectedTask)),
      filter(([{ taskId }, selected]) => selected?.id === taskId),
      map(([{ taskId }]) => TasksActions.loadTaskDetail({ taskId })),
    ),
  );

  private queueSubmissionOffline(
    taskId: string,
    values: Readonly<Record<string, unknown>>,
    attachmentIds: readonly string[],
    idempotencyKey: string,
  ) {
    return from(
      this.draftStorage.saveDraft(taskId, values, {
        queuedForSubmission: true,
        idempotencyKey,
        attachmentIds,
      }),
    ).pipe(
      catchError(() => of(void 0)),
      switchMap(() => this.refreshPendingCountActions()),
      mergeMap((connectivityAction) => [
        TasksActions.submitTaskQueuedOffline({ taskId, idempotencyKey }),
        connectivityAction,
      ]),
    );
  }

  private refreshPendingCountActions() {
    return from(this.draftStorage.listQueuedSubmissions()).pipe(
      catchError(() => of([])),
      map((queued) =>
        ConnectivityActions.pendingDraftsUpdated({
          pendingDraftsCount: queued.length,
        }),
      ),
    );
  }
}
