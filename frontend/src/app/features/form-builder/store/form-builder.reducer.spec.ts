import { describe, expect, it } from 'vitest';
import { FormBuilderActions } from './form-builder.actions';
import { formBuilderReducer } from './form-builder.reducer';
import { initialFormBuilderState } from './form-builder.state';

describe('formBuilderReducer', () => {
  it('loads form definition and initializes default schema when null', () => {
    const next = formBuilderReducer(
      initialFormBuilderState,
      FormBuilderActions.loadFormSuccess({
        form: {
          id: 'f1',
          workflowProcessId: 'p1',
          name: 'Approval',
          stepKey: 'approve',
          status: 'Draft',
          draftSchema: null,
          latestVersionNumber: null,
          updatedAt: '2026-11-26T00:00:00Z',
        },
      }),
    );

    expect(next.draftName).toBe('Approval');
    expect(next.draftSchema?.schemaVersion).toBe('1.0');
    expect(next.draftSchema?.steps.length).toBe(1);
    expect(next.dirty).toBe(false);
  });

  it('updates published status and prepends immutable version on publishVersionSuccess', () => {
    const loaded = formBuilderReducer(
      initialFormBuilderState,
      FormBuilderActions.loadFormSuccess({
        form: {
          id: 'f1',
          workflowProcessId: 'p1',
          name: 'Approval',
          stepKey: 'approve',
          status: 'Draft',
          draftSchema: null,
          latestVersionNumber: null,
          updatedAt: '2026-11-26T00:00:00Z',
        },
      }),
    );

    const published = formBuilderReducer(
      loaded,
      FormBuilderActions.publishVersionSuccess({
        version: {
          id: 'v1',
          formDefinitionId: 'f1',
          versionNumber: 1,
          publishedAt: '2026-11-26T08:00:00Z',
          publishedByUserId: 'u1',
          schema: loaded.draftSchema!,
        },
      }),
    );

    expect(published.formDefinition?.status).toBe('Published');
    expect(published.formDefinition?.latestVersionNumber).toBe(1);
    expect(published.versions).toHaveLength(1);
    expect(published.successKey).toBe('formBuilder.feedback.published');
  });
});
