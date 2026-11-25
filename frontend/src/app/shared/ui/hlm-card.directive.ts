import { computed, Directive, input } from '@angular/core';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

@Directive({
  selector: '[hlmCard]',
  standalone: true,
  host: {
    '[class]': 'computedClass()',
  },
})
export class HlmCardDirective {
  readonly class = input<string>('');

  protected readonly computedClass = computed(() =>
    twMerge(
      clsx(
        'rounded-xl border border-[var(--h2l-border-subtle)] bg-[var(--h2l-surface)] p-6',
        this.class(),
      ),
    ),
  );
}
