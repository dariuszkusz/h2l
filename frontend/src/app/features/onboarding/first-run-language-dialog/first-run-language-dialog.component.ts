import { A11yModule } from '@angular/cdk/a11y';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SupportedLanguage } from '../../../core/i18n/models/supported-language.type';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';

@Component({
  selector: 'app-first-run-language-dialog',
  standalone: true,
  imports: [A11yModule, TranslatePipe, HlmButtonDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
      data-testid="first-run-language-backdrop"
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="first-run-lang-title"
        aria-describedby="first-run-lang-desc"
        cdkTrapFocus
        [cdkTrapFocusAutoCapture]="true"
        data-testid="first-run-language-dialog"
        class="w-full max-w-md rounded-2xl border border-[var(--h2l-border)] bg-[var(--h2l-bg)] p-6 shadow-xl"
      >
        <h2 id="first-run-lang-title" class="text-xl font-semibold tracking-tight text-[var(--h2l-fg)]">
          {{ 'onboarding.languageDialog.title' | translate }}
        </h2>
        <p id="first-run-lang-desc" class="mt-2 text-sm leading-relaxed text-[var(--h2l-fg-muted)]">
          {{ 'onboarding.languageDialog.description' | translate }}
        </p>

        <div
          role="group"
          [attr.aria-label]="'onboarding.languageDialog.ariaLabel' | translate"
          class="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-3"
        >
          @for (lang of languages; track lang) {
            <button
              type="button"
              hlmBtn
              [variant]="selectedLanguage() === lang ? 'primary' : 'outline'"
              [attr.aria-pressed]="selectedLanguage() === lang"
              [attr.data-testid]="'first-run-lang-' + lang"
              (click)="onPreview(lang)"
            >
              {{ 'languages.' + lang | translate }}
            </button>
          }
        </div>

        <div class="mt-6 flex justify-end">
          <button
            type="button"
            hlmBtn
            variant="primary"
            data-testid="first-run-lang-confirm"
            (click)="onConfirm()"
          >
            {{ 'onboarding.languageDialog.confirm' | translate }}
          </button>
        </div>
      </section>
    </div>
  `,
})
export class FirstRunLanguageDialogComponent {
  readonly selectedLanguage = input.required<SupportedLanguage>();
  readonly languagePreviewed = output<SupportedLanguage>();
  readonly languageConfirmed = output<SupportedLanguage>();

  protected readonly languages: readonly SupportedLanguage[] = ['en', 'de', 'pl'];

  protected onPreview(language: SupportedLanguage): void {
    this.languagePreviewed.emit(language);
  }

  protected onConfirm(): void {
    this.languageConfirmed.emit(this.selectedLanguage());
  }
}
