import { createReducer, on } from '@ngrx/store';
import { AdminProjectsActions } from './admin-projects.actions';
import { AdminProjectsState } from './admin-projects.state';

export const initialAdminProjectsState: AdminProjectsState = {
  projects: [],
  selectedProjectId: null,
  processes: [],
  selectedProcessId: null,
  forms: [],
  selectedFormId: null,
  accessRules: [],
  loadingProjects: false,
  loadingProcesses: false,
  loadingForms: false,
  loadingAccessRules: false,
  saving: false,
  error: null,
  successMessageKey: null,
};

export const adminProjectsReducer = createReducer(
  initialAdminProjectsState,
  on(AdminProjectsActions.loadProjects, (state): AdminProjectsState => ({
    ...state,
    loadingProjects: true,
    error: null,
  })),
  on(AdminProjectsActions.loadProjectsSuccess, (state, { projects }): AdminProjectsState => ({
    ...state,
    projects,
    loadingProjects: false,
  })),
  on(AdminProjectsActions.loadProjectsFailure, (state, { error }): AdminProjectsState => ({
    ...state,
    loadingProjects: false,
    error,
  })),
  on(AdminProjectsActions.createProject, (state): AdminProjectsState => ({
    ...state,
    saving: true,
    error: null,
    successMessageKey: null,
  })),
  on(AdminProjectsActions.createProjectSuccess, (state, { project }): AdminProjectsState => ({
    ...state,
    saving: false,
    projects: [...state.projects, project],
    successMessageKey: 'adminProjects.projects.createSuccess',
  })),
  on(AdminProjectsActions.createProjectFailure, (state, { error }): AdminProjectsState => ({
    ...state,
    saving: false,
    error,
  })),
  on(AdminProjectsActions.selectProject, (state, { projectId }): AdminProjectsState => ({
    ...state,
    selectedProjectId: projectId,
    processes: projectId === state.selectedProjectId ? state.processes : [],
    selectedProcessId: projectId === state.selectedProjectId ? state.selectedProcessId : null,
    forms: projectId === state.selectedProjectId ? state.forms : [],
    selectedFormId: projectId === state.selectedProjectId ? state.selectedFormId : null,
    accessRules: projectId === state.selectedProjectId ? state.accessRules : [],
  })),
  on(AdminProjectsActions.loadProcesses, (state, { projectId }): AdminProjectsState => ({
    ...state,
    selectedProjectId: projectId,
    loadingProcesses: true,
    error: null,
  })),
  on(
    AdminProjectsActions.loadProcessesSuccess,
    (state, { projectId, processes }): AdminProjectsState => ({
      ...state,
      selectedProjectId: projectId,
      processes,
      loadingProcesses: false,
    }),
  ),
  on(AdminProjectsActions.loadProcessesFailure, (state, { error }): AdminProjectsState => ({
    ...state,
    loadingProcesses: false,
    error,
  })),
  on(AdminProjectsActions.createProcess, (state): AdminProjectsState => ({
    ...state,
    saving: true,
    error: null,
    successMessageKey: null,
  })),
  on(AdminProjectsActions.createProcessSuccess, (state, { process }): AdminProjectsState => ({
    ...state,
    saving: false,
    processes: [...state.processes, process],
    successMessageKey: 'adminProjects.processes.createSuccess',
  })),
  on(AdminProjectsActions.createProcessFailure, (state, { error }): AdminProjectsState => ({
    ...state,
    saving: false,
    error,
  })),
  on(AdminProjectsActions.selectProcess, (state, { processId }): AdminProjectsState => ({
    ...state,
    selectedProcessId: processId,
    forms: processId === state.selectedProcessId ? state.forms : [],
    selectedFormId: processId === state.selectedProcessId ? state.selectedFormId : null,
    accessRules: processId === state.selectedProcessId ? state.accessRules : [],
  })),
  on(AdminProjectsActions.loadForms, (state, { processId }): AdminProjectsState => ({
    ...state,
    selectedProcessId: processId,
    loadingForms: true,
    error: null,
  })),
  on(AdminProjectsActions.loadFormsSuccess, (state, { processId, forms }): AdminProjectsState => ({
    ...state,
    selectedProcessId: processId,
    forms,
    loadingForms: false,
  })),
  on(AdminProjectsActions.loadFormsFailure, (state, { error }): AdminProjectsState => ({
    ...state,
    loadingForms: false,
    error,
  })),
  on(AdminProjectsActions.createForm, (state): AdminProjectsState => ({
    ...state,
    saving: true,
    error: null,
    successMessageKey: null,
  })),
  on(AdminProjectsActions.createFormSuccess, (state, { form }): AdminProjectsState => ({
    ...state,
    saving: false,
    forms: [...state.forms, form],
    successMessageKey: 'adminProjects.forms.createSuccess',
  })),
  on(AdminProjectsActions.createFormFailure, (state, { error }): AdminProjectsState => ({
    ...state,
    saving: false,
    error,
  })),
  on(AdminProjectsActions.selectForm, (state, { formId }): AdminProjectsState => ({
    ...state,
    selectedFormId: formId,
    accessRules: formId === state.selectedFormId ? state.accessRules : [],
  })),
  on(AdminProjectsActions.loadAccessRules, (state, { formId }): AdminProjectsState => ({
    ...state,
    selectedFormId: formId,
    loadingAccessRules: true,
    error: null,
  })),
  on(
    AdminProjectsActions.loadAccessRulesSuccess,
    (state, { formId, rules }): AdminProjectsState => ({
      ...state,
      selectedFormId: formId,
      accessRules: rules,
      loadingAccessRules: false,
    }),
  ),
  on(AdminProjectsActions.loadAccessRulesFailure, (state, { error }): AdminProjectsState => ({
    ...state,
    loadingAccessRules: false,
    error,
  })),
  on(AdminProjectsActions.saveAccessRules, (state): AdminProjectsState => ({
    ...state,
    saving: true,
    error: null,
    successMessageKey: null,
  })),
  on(AdminProjectsActions.saveAccessRulesSuccess, (state): AdminProjectsState => ({
    ...state,
    saving: false,
    successMessageKey: 'adminProjects.acl.saveSuccess',
  })),
  on(AdminProjectsActions.saveAccessRulesFailure, (state, { error }): AdminProjectsState => ({
    ...state,
    saving: false,
    error,
  })),
  on(AdminProjectsActions.clearFeedback, (state): AdminProjectsState => ({
    ...state,
    error: null,
    successMessageKey: null,
  })),
);
