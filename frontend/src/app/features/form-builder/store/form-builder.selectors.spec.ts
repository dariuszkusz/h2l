import { describe, expect, it } from 'vitest';
import {
  selectDraftName,
  selectFormBuilderDirty,
  selectFormVersions,
} from './form-builder.selectors';
import { initialFormBuilderState } from './form-builder.state';

describe('formBuilder selectors', () => {
  it('selects draftName, dirty flag, and versions', () => {
    const state = {
      ...initialFormBuilderState,
      draftName: 'Purchase Form',
      dirty: true,
    };

    expect(selectDraftName.projector(state)).toBe('Purchase Form');
    expect(selectFormBuilderDirty.projector(state)).toBe(true);
    expect(selectFormVersions.projector(state)).toEqual([]);
  });
});
