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
  templateUrl: './tasks-page.component.html',
})
export class TasksPageComponent {}
