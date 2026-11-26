import { createFeatureSelector, createSelector } from '@ngrx/store';
import { FormBuilderState } from './form-builder.state';

export const selectFormBuilderState = createFeatureSelector<FormBuilderState>('formBuilder');

export const selectFormDefinition = createSelector(
  selectFormBuilderState,
  (state) => state.formDefinition,
);

export const selectDraftName = createSelector(
  selectFormBuilderState,
  (state) => state.draftName,
);

export const selectDraftSchema = createSelector(
  selectFormBuilderState,
  (state) => state.draftSchema,
);

export const selectFormVersions = createSelector(
  selectFormBuilderState,
  (state) => state.versions,
);

export const selectSelectedStepIndex = createSelector(
  selectFormBuilderState,
  (state) => state.selectedStepIndex,
);

export const selectSelectedComponentId = createSelector(
  selectFormBuilderState,
  (state) => state.selectedComponentId,
);

export const selectFormBuilderDirty = createSelector(
  selectFormBuilderState,
  (state) => state.dirty,
);

export const selectFormBuilderLoading = createSelector(
  selectFormBuilderState,
  (state) => state.loading,
);

export const selectFormVersionsLoading = createSelector(
  selectFormBuilderState,
  (state) => state.loadingVersions,
);

export const selectFormBuilderSaving = createSelector(
  selectFormBuilderState,
  (state) => state.saving,
);

export const selectFormBuilderPublishing = createSelector(
  selectFormBuilderState,
  (state) => state.publishing,
);

export const selectFormBuilderError = createSelector(
  selectFormBuilderState,
  (state) => state.error,
);

export const selectFormBuilderSuccessKey = createSelector(
  selectFormBuilderState,
  (state) => state.successKey,
);
