import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AdminProjectsState } from './admin-projects.state';

export const selectAdminProjectsState =
  createFeatureSelector<AdminProjectsState>('adminProjects');

export const selectProjectsList = createSelector(
  selectAdminProjectsState,
  (state) => state.projects,
);

export const selectSelectedProjectId = createSelector(
  selectAdminProjectsState,
  (state) => state.selectedProjectId,
);

export const selectSelectedProject = createSelector(
  selectProjectsList,
  selectSelectedProjectId,
  (projects, selectedId) => projects.find((p) => p.id === selectedId) ?? null,
);

export const selectProcessesList = createSelector(
  selectAdminProjectsState,
  (state) => state.processes,
);

export const selectSelectedProcessId = createSelector(
  selectAdminProjectsState,
  (state) => state.selectedProcessId,
);

export const selectSelectedProcess = createSelector(
  selectProcessesList,
  selectSelectedProcessId,
  (processes, selectedId) => processes.find((p) => p.id === selectedId) ?? null,
);

export const selectFormsList = createSelector(selectAdminProjectsState, (state) => state.forms);

export const selectSelectedFormId = createSelector(
  selectAdminProjectsState,
  (state) => state.selectedFormId,
);

export const selectSelectedForm = createSelector(
  selectFormsList,
  selectSelectedFormId,
  (forms, selectedId) => forms.find((f) => f.id === selectedId) ?? null,
);

export const selectFormAccessRules = createSelector(
  selectAdminProjectsState,
  (state) => state.accessRules,
);

export const selectAdminProjectsLoading = createSelector(
  selectAdminProjectsState,
  (state) => state.loadingProjects,
);

export const selectProcessesLoading = createSelector(
  selectAdminProjectsState,
  (state) => state.loadingProcesses,
);

export const selectFormsLoading = createSelector(
  selectAdminProjectsState,
  (state) => state.loadingForms,
);

export const selectAccessRulesLoading = createSelector(
  selectAdminProjectsState,
  (state) => state.loadingAccessRules,
);

export const selectAdminProjectsSaving = createSelector(
  selectAdminProjectsState,
  (state) => state.saving,
);

export const selectAdminProjectsError = createSelector(
  selectAdminProjectsState,
  (state) => state.error,
);

export const selectAdminProjectsSuccessKey = createSelector(
  selectAdminProjectsState,
  (state) => state.successMessageKey,
);
