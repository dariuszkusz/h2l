import { computed, Directive, input } from '@angular/core';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { HlmButtonVariant } from './hlm-button-variant.type';

@Directive({
  selector: '[hlmBtn]',
  standalone: true,
  host: {
    '[class]': 'computedClass()',
  },
})
export class HlmButtonDirective {
  readonly variant = input<HlmButtonVariant>('primary');
  readonly class = input<string>('');

  protected readonly computedClass = computed(() => {
    const base =
      'inline-flex items-center justify-center gap-2 rounded-lg px-4 py-2.5 min-h-11 min-w-11 text-sm font-medium cursor-pointer disabled:opacity-50 disabled:pointer-events-none whitespace-normal text-center';

    const variantStyles: Record<HlmButtonVariant, string> = {
      primary:
        'bg-[var(--h2l-primary)] text-[var(--h2l-primary-fg)] hover:bg-[var(--h2l-primary-hover)] border border-transparent',
      outline:
        'bg-[var(--h2l-bg)] text-[var(--h2l-fg)] border border-[var(--h2l-border)] hover:bg-[var(--h2l-surface-elevated)]',
      ghost:
        'bg-transparent text-[var(--h2l-fg)] border border-transparent hover:bg-[var(--h2l-surface-elevated)]',
    };

    return twMerge(clsx(base, variantStyles[this.variant()], this.class()));
  });
}
