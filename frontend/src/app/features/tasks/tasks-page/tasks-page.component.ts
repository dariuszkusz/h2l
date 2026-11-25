import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe, HlmButtonDirective, HlmCardDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="space-y-6">
      <header class="space-y-1">
        <h1 class="text-2xl font-semibold tracking-tight text-[var(--h2l-fg)]">
          {{ 'tasks.heading' | translate }}
        </h1>
        <p class="text-sm text-[var(--h2l-fg-muted)]">
          {{ 'tasks.description' | translate }}
        </p>
      </header>

      <div hlmCard class="flex flex-col items-start gap-4" data-testid="tasks-empty-state">
        <h2 class="text-lg font-medium text-[var(--h2l-fg)]">
          {{ 'tasks.emptyTitle' | translate }}
        </h2>
        <p class="text-sm text-[var(--h2l-fg-muted)]">
          {{ 'tasks.emptyDescription' | translate }}
        </p>
        <a routerLink="/settings" hlmBtn variant="outline" data-testid="open-settings-cta">
          {{ 'tasks.openSettingsCta' | translate }}
        </a>
      </div>
    </section>
  `,
})
export class TasksPageComponent {}
