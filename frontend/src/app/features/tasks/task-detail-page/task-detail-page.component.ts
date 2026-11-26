import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { selectIsOnline } from '../../../core/connectivity/store/connectivity.selectors';
import { selectCurrentLanguage } from '../../../core/i18n/store/i18n.selectors';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { FormFileUploadEvent } from '../../../shared/forms/form-renderer/form-file-upload-event.model';
import { FormRendererComponent } from '../../../shared/forms/form-renderer/form-renderer.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmBadgeDirective } from '../../../shared/ui/hlm-badge.directive';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { TasksActions } from '../store/tasks.actions';
import {
  selectQueuedTaskIds,
  selectSelectedTask,
  selectTaskLocalDraftValues,
  selectTasksError,
  selectTasksLoadingDetail,
  selectTasksMutating,
  selectTasksSuccessKey,
  selectUploadedAttachments,
} from '../store/tasks.selectors';

@Component({
  selector: 'app-task-detail-page',
  standalone: true,
  imports: [
    RouterLink,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmBadgeDirective,
    UiStateBannerComponent,
    FormRendererComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './task-detail-page.component.html',
})
export class TaskDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly store = inject(Store);

  protected readonly task = this.store.selectSignal(selectSelectedTask);
  protected readonly localDraftValues = this.store.selectSignal(selectTaskLocalDraftValues);
  protected readonly queuedTaskIds = this.store.selectSignal(selectQueuedTaskIds);
  protected readonly uploadedAttachments = this.store.selectSignal(selectUploadedAttachments);
  protected readonly loading = this.store.selectSignal(selectTasksLoadingDetail);
  protected readonly mutating = this.store.selectSignal(selectTasksMutating);
  protected readonly error = this.store.selectSignal(selectTasksError);
  protected readonly successKey = this.store.selectSignal(selectTasksSuccessKey);
  protected readonly isOnline = this.store.selectSignal(selectIsOnline);
  protected readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);

  protected readonly taskId = signal<string>('');

  protected readonly isQueuedOffline = computed(() =>
    this.queuedTaskIds().includes(this.taskId()),
  );

  protected readonly isReadOnlyForm = computed(() => {
    const current = this.task();
    if (!current) {
      return true;
    }
    return (
      current.status === 'Completed' ||
      current.status === 'Cancelled' ||
      current.status === 'Expired' ||
      current.canClaim
    );
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('taskId') ?? '';
    this.taskId.set(id);
    this.store.dispatch(TasksActions.clearFeedback());
    this.store.dispatch(TasksActions.loadQueuedSubmissions());
    if (id) {
      this.store.dispatch(TasksActions.loadTaskDetail({ taskId: id }));
    }
  }

  protected onClaim(): void {
    const id = this.taskId();
    if (id) {
      this.store.dispatch(TasksActions.claimTask({ taskId: id }));
    }
  }

  protected onUnclaim(): void {
    const id = this.taskId();
    if (id) {
      this.store.dispatch(TasksActions.unclaimTask({ taskId: id }));
    }
  }

  protected onValuesChanged(values: Readonly<Record<string, unknown>>): void {
    const id = this.taskId();
    if (!id || this.isReadOnlyForm()) {
      return;
    }
    this.store.dispatch(TasksActions.saveLocalDraft({ taskId: id, values }));
  }

  protected onFileSelected(event: FormFileUploadEvent): void {
    const id = this.taskId();
    if (!id || this.isReadOnlyForm()) {
      return;
    }
    this.store.dispatch(
      TasksActions.uploadAttachment({
        taskId: id,
        fieldVariableKey: event.fieldVariableKey,
        file: event.file,
      }),
    );
  }

  protected onFormSubmitted(values: Readonly<Record<string, unknown>>): void {
    const id = this.taskId();
    if (!id) {
      return;
    }
    const attachmentIds = this.uploadedAttachments().map((a) => a.id);
    this.store.dispatch(
      TasksActions.submitTask({
        taskId: id,
        values,
        attachmentIds,
      }),
    );
  }
}
