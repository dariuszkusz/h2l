import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action, provideStore } from '@ngrx/store';
import { firstValueFrom, Observable, of, toArray } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { TasksApiService } from '../../../core/api/tasks-api.service';
import { OfflineDraftStorageService } from '../../../core/connectivity/services/offline-draft-storage.service';
import { ConnectivityActions } from '../../../core/connectivity/store/connectivity.actions';
import { connectivityReducer } from '../../../core/connectivity/store/connectivity.reducer';
import { TasksSignalRService } from '../../../core/realtime/tasks-signalr.service';
import { TasksActions } from './tasks.actions';
import { TasksEffects } from './tasks.effects';
import { tasksReducer } from './tasks.reducer';

describe('TasksEffects', () => {
  it('refreshes task list automatically on 409 TASK_ALREADY_CLAIMED', async () => {
    const actions$: Observable<Action> = of(
      TasksActions.claimTaskFailure({
        taskId: 't1',
        error: {
          status: 409,
          errorCode: 'TASK_ALREADY_CLAIMED',
          messageKey: 'errors.codes.TASK_ALREADY_CLAIMED',
          detail: null,
          fieldErrors: {},
          isOffline: false,
        },
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        provideStore({ tasks: tasksReducer, connectivity: connectivityReducer }),
        TasksEffects,
        provideMockActions(() => actions$),
        { provide: TasksApiService, useValue: {} },
        { provide: OfflineDraftStorageService, useValue: {} },
        { provide: TasksSignalRService, useValue: { taskEvents$: of(), connect: vi.fn(), disconnect: vi.fn() } },
      ],
    });

    const effects = TestBed.inject(TasksEffects);
    const result = await firstValueFrom(effects.refreshListOnClaimConflict$);
    expect(result.type).toBe(TasksActions.loadTasks.type);
  });

  it('clears IndexedDB draft on unclaimTask (RODO)', async () => {
    const actions$: Observable<Action> = of(TasksActions.unclaimTask({ taskId: 't1' }));
    const deleteDraft = vi.fn().mockResolvedValue(undefined);
    const listQueuedSubmissions = vi.fn().mockResolvedValue([]);
    const unclaimTask = vi.fn(() =>
      of({
        id: 't1',
        status: 'Pending' as const,
        formVersionId: 'fv1',
        formVersionNumber: 1,
        schema: {
          schemaVersion: '1.0' as const,
          defaultLanguage: 'en' as const,
          layoutMode: 'single' as const,
          steps: [],
        },
        prefilledData: {},
        assignedUserId: null,
        claimedByUserId: null,
        canClaim: true,
        canUnclaim: false,
        sensitiveDataMode: 'ClaimCheck' as const,
        expiresAt: null,
        createdAt: '2026-11-26T00:00:00Z',
        completedAt: null,
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        provideStore({ tasks: tasksReducer, connectivity: connectivityReducer }),
        TasksEffects,
        provideMockActions(() => actions$),
        { provide: TasksApiService, useValue: { unclaimTask } },
        { provide: OfflineDraftStorageService, useValue: { deleteDraft, listQueuedSubmissions } },
        { provide: TasksSignalRService, useValue: { taskEvents$: of(), connect: vi.fn(), disconnect: vi.fn() } },
      ],
    });

    const effects = TestBed.inject(TasksEffects);
    const results = await firstValueFrom(effects.unclaimTask$.pipe(toArray()));

    expect(deleteDraft).toHaveBeenCalledWith('t1');
    expect(results.some((a) => a.type === TasksActions.unclaimTaskSuccess.type)).toBe(true);
    expect(results.some((a) => a.type === ConnectivityActions.pendingDraftsUpdated.type)).toBe(true);
  });

  it('syncs queued offline submissions with Idempotency-Key and deletes IndexedDB record on success', async () => {
    const actions$: Observable<Action> = of(TasksActions.syncQueuedSubmissions());
    const deleteDraft = vi.fn().mockResolvedValue(undefined);
    const listQueuedSubmissions = vi
      .fn()
      .mockResolvedValueOnce([
        {
          taskId: 't-offline',
          payload: { softwareName: 'Rider' },
          updatedAt: '2026-11-26T08:00:00Z',
          queuedForSubmission: true,
          idempotencyKey: 'idem-offline-1',
          attachmentIds: [],
        },
      ])
      .mockResolvedValueOnce([]);
    const submitTask = vi.fn(() =>
      of({
        taskId: 't-offline',
        submissionId: 'sub-1',
        status: 'Completed' as const,
        submittedAt: '2026-11-26T08:05:00Z',
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        provideStore({ tasks: tasksReducer, connectivity: connectivityReducer }),
        TasksEffects,
        provideMockActions(() => actions$),
        { provide: TasksApiService, useValue: { submitTask } },
        { provide: OfflineDraftStorageService, useValue: { deleteDraft, listQueuedSubmissions } },
        { provide: TasksSignalRService, useValue: { taskEvents$: of(), connect: vi.fn(), disconnect: vi.fn() } },
      ],
    });

    const effects = TestBed.inject(TasksEffects);
    const results = await firstValueFrom(effects.syncQueuedSubmissions$.pipe(toArray()));

    expect(submitTask).toHaveBeenCalledWith(
      't-offline',
      { values: { softwareName: 'Rider' }, attachmentIds: [] },
      'idem-offline-1',
    );
    expect(deleteDraft).toHaveBeenCalledWith('t-offline');
    expect(results.some((a) => a.type === TasksActions.syncQueuedSubmissionCompleted.type)).toBe(true);
  });
});
