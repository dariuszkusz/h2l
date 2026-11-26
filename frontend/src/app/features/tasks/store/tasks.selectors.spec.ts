import { describe, expect, it } from 'vitest';
import { selectQueuedTaskIds, selectTasksScope } from './tasks.selectors';
import { initialTasksState } from './tasks.state';

describe('tasks selectors', () => {
  it('selects scope and queuedTaskIds', () => {
    const state = {
      ...initialTasksState,
      scope: 'group_queue' as const,
      queuedTaskIds: ['t1'],
    };

    expect(selectTasksScope.projector(state)).toBe('group_queue');
    expect(selectQueuedTaskIds.projector(state)).toEqual(['t1']);
  });
});
