import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmBadgeVariant } from '../../../shared/ui/hlm-badge-variant.type';
import { HlmBadgeDirective } from '../../../shared/ui/hlm-badge.directive';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { TasksActions } from '../store/tasks.actions';
import {
  selectQueuedTaskIds,
  selectTasksError,
  selectTasksItems,
  selectTasksLoadingList,
  selectTasksMutating,
  selectTasksScope,
  selectTasksSuccessKey,
} from '../store/tasks.selectors';

type TaskListScope = components['schemas']['TaskListScope'];
type HumanTaskStatus = components['schemas']['HumanTaskStatus'];

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  imports: [
    RouterLink,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmBadgeDirective,
    UiStateBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tasks-page.component.html',
})
export class TasksPageComponent implements OnInit {
  private readonly store = inject(Store);

  protected readonly items = this.store.selectSignal(selectTasksItems);
  protected readonly scope = this.store.selectSignal(selectTasksScope);
  protected readonly queuedTaskIds = this.store.selectSignal(selectQueuedTaskIds);
  protected readonly loading = this.store.selectSignal(selectTasksLoadingList);
  protected readonly mutating = this.store.selectSignal(selectTasksMutating);
  protected readonly error = this.store.selectSignal(selectTasksError);
  protected readonly successKey = this.store.selectSignal(selectTasksSuccessKey);

  protected readonly scopes: readonly TaskListScope[] = ['all', 'assigned', 'group_queue'];

  ngOnInit(): void {
    this.store.dispatch(TasksActions.clearFeedback());
    this.store.dispatch(TasksActions.loadQueuedSubmissions());
    this.store.dispatch(TasksActions.loadTasks({ scope: this.scope() }));
  }

  protected onSelectScope(scope: TaskListScope): void {
    this.store.dispatch(TasksActions.setScope({ scope }));
  }

  protected onClaim(taskId: string): void {
    this.store.dispatch(TasksActions.claimTask({ taskId }));
  }

  protected onUnclaim(taskId: string): void {
    this.store.dispatch(TasksActions.unclaimTask({ taskId }));
  }

  protected isQueuedOffline(taskId: string): boolean {
    return this.queuedTaskIds().includes(taskId);
  }

  protected statusBadgeVariant(status: HumanTaskStatus): HlmBadgeVariant {
    switch (status) {
      case 'Completed':
        return 'success';
      case 'InProgress':
        return 'info';
      case 'NeedsAssignment':
        return 'warning';
      case 'Cancelled':
      case 'Expired':
        return 'danger';
      default:
        return 'neutral';
    }
  }
}
