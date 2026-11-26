import { computed, Directive, input } from '@angular/core';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

@Directive({
  selector: '[hlmInput]',
  standalone: true,
  host: {
    '[class]': 'computedClass()',
  },
})
export class HlmInputDirective {
  readonly hasError = input<boolean>(false);
  readonly class = input<string>('');

  protected readonly computedClass = computed(() => {
    const base =
      'block w-full min-h-11 rounded-lg border px-3.5 py-2.5 text-sm bg-[var(--h2l-bg)] text-[var(--h2l-fg)] placeholder:text-[var(--h2l-fg-muted)] transition-colors disabled:opacity-50 disabled:cursor-not-allowed';
    const borderClass = this.hasError()
      ? 'border-[var(--h2l-danger-fg)]'
      : 'border-[var(--h2l-border)]';

    return twMerge(clsx(base, borderClass, this.class()));
  });
}
