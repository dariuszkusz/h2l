import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { BrandLogoSize } from './brand-logo-size.type';
import { BrandLogoVariant } from './brand-logo-variant.type';

let nextBrandLogoInstanceId = 0;

@Component({
  selector: 'app-brand-logo',
  standalone: true,
  imports: [TranslatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './brand-logo.component.html',
})
export class BrandLogoComponent {
  readonly variant = input<BrandLogoVariant>('full');
  readonly size = input<BrandLogoSize>('md');
  readonly showTagline = input<boolean>(false);
  readonly decorative = input<boolean>(false);

  private readonly instanceId = ++nextBrandLogoInstanceId;
  protected readonly gradientId = `h2l-brand-grad-${this.instanceId}`;
  protected readonly maskId = `h2l-brand-mask-${this.instanceId}`;

  protected readonly showMark = computed(
    () => this.variant() === 'full' || this.variant() === 'mark',
  );

  protected readonly showWordmark = computed(
    () => this.variant() === 'full' || this.variant() === 'wordmark',
  );

  protected readonly markSizeClass = computed(() => {
    switch (this.size()) {
      case 'sm':
        return 'h-6 w-auto';
      case 'lg':
        return 'h-12 w-auto';
      default:
        return 'h-8 w-auto';
    }
  });

  protected readonly wordmarkSizeClass = computed(() => {
    switch (this.size()) {
      case 'sm':
        return 'text-lg';
      case 'lg':
        return 'text-3xl';
      default:
        return 'text-2xl';
    }
  });

  protected readonly taglineSizeClass = computed(() => {
    switch (this.size()) {
      case 'sm':
        return 'text-[9px] tracking-[0.16em]';
      case 'lg':
        return 'text-xs tracking-[0.2em]';
      default:
        return 'text-[10px] tracking-[0.18em]';
    }
  });
}
