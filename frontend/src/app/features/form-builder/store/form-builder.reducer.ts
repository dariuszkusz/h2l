import { createReducer, on } from '@ngrx/store';
import { createDefaultFormSchema } from '../../../shared/forms/utils/create-default-form-schema';
import { FormBuilderActions } from './form-builder.actions';
import { initialFormBuilderState } from './form-builder.state';

export const formBuilderReducer = createReducer(
  initialFormBuilderState,

  on(FormBuilderActions.loadForm, (state) => ({
    ...state,
    loading: true,
    error: null,
    successKey: null,
  })),
  on(FormBuilderActions.loadFormSuccess, (state, { form }) => ({
    ...state,
    loading: false,
    formDefinition: form,
    draftName: form.name,
    draftSchema: form.draftSchema ?? createDefaultFormSchema(),
    selectedStepIndex: 0,
    selectedComponentId: null,
    dirty: false,
  })),
  on(FormBuilderActions.loadFormFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),

  on(FormBuilderActions.loadVersions, (state) => ({
    ...state,
    loadingVersions: true,
  })),
  on(FormBuilderActions.loadVersionsSuccess, (state, { versions }) => ({
    ...state,
    loadingVersions: false,
    versions,
  })),
  on(FormBuilderActions.loadVersionsFailure, (state, { error }) => ({
    ...state,
    loadingVersions: false,
    error,
  })),

  on(FormBuilderActions.updateDraftName, (state, { name }) => ({
    ...state,
    draftName: name,
    dirty: true,
    successKey: null,
  })),
  on(FormBuilderActions.updateDraftSchema, (state, { schema }) => ({
    ...state,
    draftSchema: schema,
    dirty: true,
    successKey: null,
  })),
  on(FormBuilderActions.selectStep, (state, { stepIndex }) => ({
    ...state,
    selectedStepIndex: stepIndex,
    selectedComponentId: null,
  })),
  on(FormBuilderActions.selectComponent, (state, { componentId }) => ({
    ...state,
    selectedComponentId: componentId,
  })),

  on(FormBuilderActions.saveDraft, (state) => ({
    ...state,
    saving: true,
    error: null,
    successKey: null,
  })),
  on(FormBuilderActions.saveDraftSuccess, (state, { form }) => ({
    ...state,
    saving: false,
    formDefinition: form,
    draftName: form.name,
    draftSchema: form.draftSchema ?? state.draftSchema,
    dirty: false,
    successKey: 'formBuilder.feedback.draftSaved',
  })),
  on(FormBuilderActions.saveDraftFailure, (state, { error }) => ({
    ...state,
    saving: false,
    error,
  })),

  on(FormBuilderActions.publishVersion, (state) => ({
    ...state,
    publishing: true,
    error: null,
    successKey: null,
  })),
  on(FormBuilderActions.publishVersionSuccess, (state, { version }) => ({
    ...state,
    publishing: false,
    formDefinition: state.formDefinition
      ? {
          ...state.formDefinition,
          status: 'Published',
          latestVersionNumber: version.versionNumber,
        }
      : null,
    versions: [
      {
        id: version.id,
        versionNumber: version.versionNumber,
        publishedAt: version.publishedAt,
        publishedByUserId: version.publishedByUserId,
        schema: version.schema,
      },
      ...state.versions.filter((v) => v.id !== version.id),
    ],
    successKey: 'formBuilder.feedback.published',
  })),
  on(FormBuilderActions.publishVersionFailure, (state, { error }) => ({
    ...state,
    publishing: false,
    error,
  })),

  on(FormBuilderActions.clearFeedback, (state) => ({
    ...state,
    error: null,
    successKey: null,
  })),
  on(FormBuilderActions.reset, () => initialFormBuilderState),
);
