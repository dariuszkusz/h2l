import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { UiStateBannerKind } from './ui-state-banner-kind.type';

@Component({
  selector: 'app-ui-state-banner',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ui-state-banner.component.html',
})
export class UiStateBannerComponent {
  readonly kind = input.required<UiStateBannerKind>();
  readonly title = input<string>('');
  readonly message = input<string>('');
  readonly detail = input<string | null>(null);
  readonly fieldErrors = input<Readonly<Record<string, readonly string[]>> | null>(null);
  readonly testId = input<string>('ui-state-banner');

  protected readonly fieldErrorEntries = computed(() => {
    const errors = this.fieldErrors();
    if (!errors) {
      return [];
    }

    return Object.entries(errors).map(([field, messages]) => ({
      field,
      messages,
    }));
  });
}
