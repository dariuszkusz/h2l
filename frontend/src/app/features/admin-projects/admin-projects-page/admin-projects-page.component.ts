import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { selectCurrentUserRole } from '../../../core/auth/store/auth.selectors';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmBadgeDirective } from '../../../shared/ui/hlm-badge.directive';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { HlmInputDirective } from '../../../shared/ui/hlm-input.directive';
import { HlmLabelDirective } from '../../../shared/ui/hlm-label.directive';
import { AdminUsersActions } from '../../admin-users/store/admin-users.actions';
import {
  selectAdminGroupsList,
  selectAdminUsersList,
} from '../../admin-users/store/admin-users.selectors';
import { AdminProjectsActions } from '../store/admin-projects.actions';
import {
  selectAccessRulesLoading,
  selectAdminProjectsError,
  selectAdminProjectsLoading,
  selectAdminProjectsSaving,
  selectAdminProjectsSuccessKey,
  selectFormAccessRules,
  selectFormsList,
  selectFormsLoading,
  selectProcessesList,
  selectProcessesLoading,
  selectProjectsList,
  selectSelectedForm,
  selectSelectedFormId,
  selectSelectedProcess,
  selectSelectedProcessId,
  selectSelectedProject,
  selectSelectedProjectId,
} from '../store/admin-projects.selectors';

type WorkflowProcessStatus = components['schemas']['WorkflowProcessStatus'];
type FormAccessScope = components['schemas']['FormAccessScope'];
type DraftAccessRule = {
  readonly groupId: string | null;
  readonly userId: string | null;
  readonly accessScope: FormAccessScope;
};

@Component({
  selector: 'app-admin-projects-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmBadgeDirective,
    HlmInputDirective,
    HlmLabelDirective,
    UiStateBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-projects-page.component.html',
  host: {
    '(document:keydown.escape)': 'onEscapeKey()',
  },
})
export class AdminProjectsPageComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  protected readonly currentUserRole = this.store.selectSignal(selectCurrentUserRole);
  protected readonly projects = this.store.selectSignal(selectProjectsList);
  protected readonly selectedProjectId = this.store.selectSignal(selectSelectedProjectId);
  protected readonly selectedProject = this.store.selectSignal(selectSelectedProject);
  protected readonly processes = this.store.selectSignal(selectProcessesList);
  protected readonly selectedProcessId = this.store.selectSignal(selectSelectedProcessId);
  protected readonly selectedProcess = this.store.selectSignal(selectSelectedProcess);
  protected readonly forms = this.store.selectSignal(selectFormsList);
  protected readonly selectedFormId = this.store.selectSignal(selectSelectedFormId);
  protected readonly selectedForm = this.store.selectSignal(selectSelectedForm);
  protected readonly serverAccessRules = this.store.selectSignal(selectFormAccessRules);

  protected readonly groups = this.store.selectSignal(selectAdminGroupsList);
  protected readonly users = this.store.selectSignal(selectAdminUsersList);

  protected readonly loadingProjects = this.store.selectSignal(selectAdminProjectsLoading);
  protected readonly loadingProcesses = this.store.selectSignal(selectProcessesLoading);
  protected readonly loadingForms = this.store.selectSignal(selectFormsLoading);
  protected readonly loadingAccessRules = this.store.selectSignal(selectAccessRulesLoading);
  protected readonly saving = this.store.selectSignal(selectAdminProjectsSaving);
  protected readonly error = this.store.selectSignal(selectAdminProjectsError);
  protected readonly successKey = this.store.selectSignal(selectAdminProjectsSuccessKey);

  protected readonly projectModalOpen = signal(false);
  protected readonly processModalOpen = signal(false);
  protected readonly formModalOpen = signal(false);

  protected readonly draftRules = signal<readonly DraftAccessRule[]>([]);
  protected readonly aclMode = signal<FormAccessScope>('WholeGroup');
  protected readonly aclGroupId = signal<string>('');
  protected readonly aclUserId = signal<string>('');

  protected readonly processStatuses: readonly WorkflowProcessStatus[] = [
    'Active',
    'Draft',
    'Archived',
  ];

  protected readonly aclScopes: readonly FormAccessScope[] = [
    'WholeGroup',
    'GroupUserSubset',
    'DirectUser',
  ];

  protected onEscapeKey(): void {
    if (this.projectModalOpen()) {
      this.projectModalOpen.set(false);
    } else if (this.processModalOpen()) {
      this.processModalOpen.set(false);
    } else if (this.formModalOpen()) {
      this.formModalOpen.set(false);
    }
  }

  protected readonly isAdmin = computed(() => this.currentUserRole() === 'Admin');

  protected readonly usersInSelectedAclGroup = computed(() => {
    const groupId = this.aclGroupId();
    if (!groupId) {
      return [];
    }
    return this.users().filter((u) => u.groupIds.includes(groupId));
  });

  protected readonly projectForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(2000)]],
  });

  protected readonly processForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    externalProcessKey: ['', [Validators.required, Validators.maxLength(200)]],
    status: ['Active' as WorkflowProcessStatus, [Validators.required]],
  });

  protected readonly draftFormForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    stepKey: ['', [Validators.required, Validators.maxLength(200)]],
  });

  constructor() {
    effect(() => {
      const rules = this.serverAccessRules();
      this.draftRules.set(
        rules.map((r) => ({
          groupId: r.groupId ?? null,
          userId: r.userId ?? null,
          accessScope: r.accessScope,
        })),
      );
    });
  }

  ngOnInit(): void {
    this.store.dispatch(AdminProjectsActions.clearFeedback());
    this.store.dispatch(AdminProjectsActions.loadProjects());
    this.store.dispatch(AdminUsersActions.loadGroups());
    if (this.currentUserRole() === 'Admin') {
      this.store.dispatch(AdminUsersActions.loadUsers());
    }
  }

  protected openProjectModal(): void {
    this.store.dispatch(AdminProjectsActions.clearFeedback());
    this.projectForm.reset({ name: '', description: '' });
    this.projectModalOpen.set(true);
  }

  protected closeProjectModal(): void {
    this.projectModalOpen.set(false);
  }

  protected onCreateProjectSubmit(): void {
    if (this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }

    const raw = this.projectForm.getRawValue();
    const desc = raw.description.trim();
    this.store.dispatch(
      AdminProjectsActions.createProject({
        request: {
          name: raw.name.trim(),
          description: desc.length > 0 ? desc : null,
        },
      }),
    );
    this.projectModalOpen.set(false);
  }

  protected onSelectProject(projectId: string): void {
    this.store.dispatch(AdminProjectsActions.selectProject({ projectId }));
    this.store.dispatch(AdminProjectsActions.loadProcesses({ projectId }));
  }

  protected openProcessModal(): void {
    this.store.dispatch(AdminProjectsActions.clearFeedback());
    this.processForm.reset({ name: '', externalProcessKey: '', status: 'Active' });
    this.processModalOpen.set(true);
  }

  protected closeProcessModal(): void {
    this.processModalOpen.set(false);
  }

  protected onCreateProcessSubmit(): void {
    const projectId = this.selectedProjectId();
    if (!projectId || this.processForm.invalid) {
      this.processForm.markAllAsTouched();
      return;
    }

    const raw = this.processForm.getRawValue();
    this.store.dispatch(
      AdminProjectsActions.createProcess({
        projectId,
        request: {
          name: raw.name.trim(),
          externalProcessKey: raw.externalProcessKey.trim(),
          status: raw.status,
        },
      }),
    );
    this.processModalOpen.set(false);
  }

  protected onSelectProcess(processId: string): void {
    this.store.dispatch(AdminProjectsActions.selectProcess({ processId }));
    this.store.dispatch(AdminProjectsActions.loadForms({ processId }));
  }

  protected openFormModal(): void {
    this.store.dispatch(AdminProjectsActions.clearFeedback());
    this.draftFormForm.reset({ name: '', stepKey: '' });
    this.formModalOpen.set(true);
  }

  protected closeFormModal(): void {
    this.formModalOpen.set(false);
  }

  protected onCreateFormSubmit(): void {
    const processId = this.selectedProcessId();
    if (!processId || this.draftFormForm.invalid) {
      this.draftFormForm.markAllAsTouched();
      return;
    }

    const raw = this.draftFormForm.getRawValue();
    this.store.dispatch(
      AdminProjectsActions.createForm({
        processId,
        request: {
          name: raw.name.trim(),
          stepKey: raw.stepKey.trim(),
          draftSchemaJsonb: {},
        },
      }),
    );
    this.formModalOpen.set(false);
  }

  protected onSelectForm(formId: string): void {
    this.store.dispatch(AdminProjectsActions.selectForm({ formId }));
    this.store.dispatch(AdminProjectsActions.loadAccessRules({ formId }));
  }

  protected onAclModeChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.aclMode.set(select.value as FormAccessScope);
    this.aclGroupId.set('');
    this.aclUserId.set('');
  }

  protected onAclGroupChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.aclGroupId.set(select.value);
    this.aclUserId.set('');
  }

  protected onAclUserChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.aclUserId.set(select.value);
  }

  protected canAddRule(): boolean {
    const mode = this.aclMode();
    if (mode === 'WholeGroup') {
      return this.aclGroupId().length > 0;
    }
    if (mode === 'GroupUserSubset') {
      return this.aclGroupId().length > 0 && this.aclUserId().length > 0;
    }
    return this.aclUserId().length > 0;
  }

  protected onAddAclRule(): void {
    if (!this.canAddRule()) {
      return;
    }

    const mode = this.aclMode();
    const newRule: DraftAccessRule =
      mode === 'WholeGroup'
        ? { groupId: this.aclGroupId(), userId: null, accessScope: 'WholeGroup' }
        : mode === 'GroupUserSubset'
          ? {
              groupId: this.aclGroupId(),
              userId: this.aclUserId(),
              accessScope: 'GroupUserSubset',
            }
          : { groupId: null, userId: this.aclUserId(), accessScope: 'DirectUser' };

    const duplicate = this.draftRules().some(
      (r) => r.groupId === newRule.groupId && r.userId === newRule.userId,
    );
    if (!duplicate) {
      this.draftRules.set([...this.draftRules(), newRule]);
    }
    this.aclUserId.set('');
  }

  protected onRemoveAclRule(index: number): void {
    this.draftRules.set(this.draftRules().filter((_, idx) => idx !== index));
  }

  protected onSaveAclRules(): void {
    const formId = this.selectedFormId();
    if (!formId) {
      return;
    }

    this.store.dispatch(
      AdminProjectsActions.saveAccessRules({
        formId,
        request: {
          rules: this.draftRules().map((r) => ({
            groupId: r.groupId,
            userId: r.userId,
          })),
        },
      }),
    );
  }

  protected resolveGroupLabel(groupId: string | null): string {
    if (!groupId) {
      return '';
    }
    const found = this.groups().find((g) => g.id === groupId);
    return found ? `${found.name} (${found.code})` : groupId;
  }

  protected resolveUserLabel(userId: string | null): string {
    if (!userId) {
      return '';
    }
    const found = this.users().find((u) => u.id === userId);
    return found ? found.email : userId;
  }

  protected onDialogKeydown(event: KeyboardEvent, kind: 'project' | 'process' | 'form'): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      if (kind === 'project') {
        this.closeProjectModal();
      } else if (kind === 'process') {
        this.closeProcessModal();
      } else {
        this.closeFormModal();
      }
    }
  }
}
