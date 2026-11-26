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
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmBadgeDirective } from '../../../shared/ui/hlm-badge.directive';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { HlmInputDirective } from '../../../shared/ui/hlm-input.directive';
import { HlmLabelDirective } from '../../../shared/ui/hlm-label.directive';
import { AdminUsersActions } from '../store/admin-users.actions';
import {
  selectAdminGroupsList,
  selectAdminUsersError,
  selectAdminUsersList,
  selectAdminUsersLoading,
  selectAdminUsersSaving,
  selectAdminUsersSuccessKey,
  selectGroupMembers,
  selectGroupMembersLoading,
  selectLastInvitedUser,
  selectSelectedGroup,
  selectSelectedGroupId,
} from '../store/admin-users.selectors';

type SystemRole = components['schemas']['SystemRole'];

@Component({
  selector: 'app-admin-users-page',
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
  templateUrl: './admin-users-page.component.html',
  host: {
    '(document:keydown.escape)': 'onEscapeKey()',
  },
})
export class AdminUsersPageComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  protected readonly users = this.store.selectSignal(selectAdminUsersList);
  protected readonly groups = this.store.selectSignal(selectAdminGroupsList);
  protected readonly selectedGroupId = this.store.selectSignal(selectSelectedGroupId);
  protected readonly selectedGroup = this.store.selectSignal(selectSelectedGroup);
  protected readonly groupMembers = this.store.selectSignal(selectGroupMembers);
  protected readonly loading = this.store.selectSignal(selectAdminUsersLoading);
  protected readonly loadingMembers = this.store.selectSignal(selectGroupMembersLoading);
  protected readonly saving = this.store.selectSignal(selectAdminUsersSaving);
  protected readonly lastInvitedUser = this.store.selectSignal(selectLastInvitedUser);
  protected readonly error = this.store.selectSignal(selectAdminUsersError);
  protected readonly successKey = this.store.selectSignal(selectAdminUsersSuccessKey);

  protected readonly inviteModalOpen = signal(false);
  protected readonly createGroupModalOpen = signal(false);
  protected readonly copiedLink = signal(false);
  protected readonly selectedInviteGroupIds = signal<readonly string[]>([]);
  protected readonly memberToAddId = signal<string>('');

  protected readonly roles: readonly SystemRole[] = ['EndUser', 'ProcessDesigner', 'Admin'];

  protected readonly availableUsersForGroup = computed(() => {
    const memberIds = new Set(this.groupMembers().map((m) => m.userId));
    return this.users().filter((u) => !memberIds.has(u.id));
  });

  protected readonly inviteForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    externalId: ['', [Validators.maxLength(200)]],
    systemRole: ['EndUser' as SystemRole, [Validators.required]],
  });

  protected readonly groupForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    code: ['', [Validators.required, Validators.maxLength(100)]],
  });

  constructor() {
    effect(() => {
      const key = this.successKey();
      if (key === 'adminUsers.groups.createSuccess') {
        this.createGroupModalOpen.set(false);
      }
    });
  }

  ngOnInit(): void {
    this.store.dispatch(AdminUsersActions.clearFeedback());
    this.store.dispatch(AdminUsersActions.loadUsers());
    this.store.dispatch(AdminUsersActions.loadGroups());
  }

  protected openInviteModal(): void {
    this.store.dispatch(AdminUsersActions.clearFeedback());
    this.inviteForm.reset({ email: '', externalId: '', systemRole: 'EndUser' });
    this.selectedInviteGroupIds.set([]);
    this.copiedLink.set(false);
    this.inviteModalOpen.set(true);
  }

  protected closeInviteModal(): void {
    this.inviteModalOpen.set(false);
  }

  protected toggleInviteGroup(groupId: string): void {
    const current = this.selectedInviteGroupIds();
    if (current.includes(groupId)) {
      this.selectedInviteGroupIds.set(current.filter((id) => id !== groupId));
    } else {
      this.selectedInviteGroupIds.set([...current, groupId]);
    }
  }

  protected onInviteSubmit(): void {
    if (this.inviteForm.invalid) {
      this.inviteForm.markAllAsTouched();
      return;
    }

    const raw = this.inviteForm.getRawValue();
    const trimmedExternalId = raw.externalId.trim();
    this.store.dispatch(
      AdminUsersActions.inviteUser({
        request: {
          email: raw.email.trim(),
          externalId: trimmedExternalId.length > 0 ? trimmedExternalId : null,
          systemRole: raw.systemRole,
          groupIds: [...this.selectedInviteGroupIds()],
        },
      }),
    );
    this.inviteModalOpen.set(false);
  }

  protected openCreateGroupModal(): void {
    this.store.dispatch(AdminUsersActions.clearFeedback());
    this.groupForm.reset({ name: '', code: '' });
    this.createGroupModalOpen.set(true);
  }

  protected closeCreateGroupModal(): void {
    this.createGroupModalOpen.set(false);
  }

  protected onCreateGroupSubmit(): void {
    if (this.groupForm.invalid) {
      this.groupForm.markAllAsTouched();
      return;
    }

    const raw = this.groupForm.getRawValue();
    this.store.dispatch(
      AdminUsersActions.createGroup({
        request: {
          name: raw.name.trim(),
          code: raw.code.trim(),
        },
      }),
    );
  }

  protected onSelectGroup(groupId: string): void {
    this.memberToAddId.set('');
    this.store.dispatch(AdminUsersActions.loadGroupMembers({ groupId }));
  }

  protected onMemberSelectChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.memberToAddId.set(select.value);
  }

  protected onAddGroupMember(): void {
    const groupId = this.selectedGroupId();
    const userId = this.memberToAddId();
    if (!groupId || !userId) {
      return;
    }

    this.store.dispatch(
      AdminUsersActions.addGroupMember({
        groupId,
        request: { userId },
      }),
    );
    this.memberToAddId.set('');
  }

  protected onRemoveGroupMember(userId: string): void {
    const groupId = this.selectedGroupId();
    if (!groupId) {
      return;
    }

    this.store.dispatch(AdminUsersActions.removeGroupMember({ groupId, userId }));
  }

  protected onCopyActivationLink(link: string): void {
    if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
      void navigator.clipboard.writeText(link).then(() => {
        this.copiedLink.set(true);
      });
    } else {
      this.copiedLink.set(true);
    }
  }

  protected onEscapeKey(): void {
    if (this.inviteModalOpen()) {
      this.closeInviteModal();
    } else if (this.createGroupModalOpen()) {
      this.closeCreateGroupModal();
    }
  }

  protected onDialogKeydown(event: KeyboardEvent, kind: 'invite' | 'group'): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      if (kind === 'invite') {
        this.closeInviteModal();
      } else {
        this.closeCreateGroupModal();
      }
    }
  }
}
