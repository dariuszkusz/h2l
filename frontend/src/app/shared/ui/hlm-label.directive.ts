import { computed, Directive, input } from '@angular/core';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

@Directive({
  selector: '[hlmLabel]',
  standalone: true,
  host: {
    '[class]': 'computedClass()',
  },
})
export class HlmLabelDirective {
  readonly class = input<string>('');

  protected readonly computedClass = computed(() =>
    twMerge(clsx('block text-sm font-medium text-[var(--h2l-fg)]', this.class())),
  );
}
