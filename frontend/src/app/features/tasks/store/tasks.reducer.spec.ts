import { describe, expect, it } from 'vitest';
import { AuthActions } from '../../../core/auth/store/auth.actions';
import { TasksActions } from './tasks.actions';
import { tasksReducer } from './tasks.reducer';
import { initialTasksState } from './tasks.state';

describe('tasksReducer', () => {
  it('tracks offline queued submissions and clears state on logoutCompleted', () => {
    const queued = tasksReducer(
      initialTasksState,
      TasksActions.submitTaskQueuedOffline({ taskId: 't1', idempotencyKey: 'key-1' }),
    );
    expect(queued.queuedTaskIds).toEqual(['t1']);
    expect(queued.successKey).toBe('tasks.feedback.queuedOffline');

    const synced = tasksReducer(
      queued,
      TasksActions.syncQueuedSubmissionCompleted({ taskId: 't1' }),
    );
    expect(synced.queuedTaskIds).toEqual([]);

    const reset = tasksReducer(queued, AuthActions.logoutCompleted());
    expect(reset).toEqual(initialTasksState);
  });
});
