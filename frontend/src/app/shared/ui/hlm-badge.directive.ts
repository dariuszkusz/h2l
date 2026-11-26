import { computed, Directive, input } from '@angular/core';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { HlmBadgeVariant } from './hlm-badge-variant.type';

@Directive({
  selector: '[hlmBadge]',
  standalone: true,
  host: {
    '[class]': 'computedClass()',
  },
})
export class HlmBadgeDirective {
  readonly variant = input<HlmBadgeVariant>('neutral');
  readonly class = input<string>('');

  protected readonly computedClass = computed(() => {
    const base =
      'inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-semibold';

    const variants: Record<HlmBadgeVariant, string> = {
      neutral:
        'border-[var(--h2l-border-subtle)] bg-[var(--h2l-surface-elevated)] text-[var(--h2l-fg)]',
      info:
        'border-[var(--h2l-border-subtle)] bg-[var(--h2l-info-bg)] text-[var(--h2l-info-fg)]',
      success:
        'border-[var(--h2l-border-subtle)] bg-[var(--h2l-status-online-bg)] text-[var(--h2l-status-online-fg)]',
      warning:
        'border-[var(--h2l-border)] bg-[var(--h2l-warning-bg)] text-[var(--h2l-warning-fg)]',
      danger:
        'border-[var(--h2l-danger-fg)] bg-[var(--h2l-danger-bg)] text-[var(--h2l-danger-fg)]',
    };

    return twMerge(clsx(base, variants[this.variant()], this.class()));
  });
}
