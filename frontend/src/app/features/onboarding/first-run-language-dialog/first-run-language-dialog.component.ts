import { A11yModule } from '@angular/cdk/a11y';
import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  OnDestroy,
  OnInit,
  output,
} from '@angular/core';
import { SupportedLanguage } from '../../../core/i18n/models/supported-language.type';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';

@Component({
  selector: 'app-first-run-language-dialog',
  standalone: true,
  imports: [A11yModule, TranslatePipe, HlmButtonDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './first-run-language-dialog.component.html',
  host: {
    '(document:keydown.escape)': 'onEscapeKey($event)',
  },
})
export class FirstRunLanguageDialogComponent implements OnInit, OnDestroy {
  private readonly document = inject(DOCUMENT);
  private previouslyFocusedElement: HTMLElement | null = null;

  readonly selectedLanguage = input.required<SupportedLanguage>();
  readonly languagePreviewed = output<SupportedLanguage>();
  readonly languageConfirmed = output<SupportedLanguage>();

  protected readonly languages: readonly SupportedLanguage[] = ['en', 'de', 'pl'];

  ngOnInit(): void {
    const active = this.document.activeElement;
    if (active instanceof HTMLElement) {
      this.previouslyFocusedElement = active;
    }
  }

  ngOnDestroy(): void {
    if (this.previouslyFocusedElement && this.previouslyFocusedElement.isConnected) {
      this.previouslyFocusedElement.focus();
    }
  }

  protected onPreview(language: SupportedLanguage): void {
    this.languagePreviewed.emit(language);
  }

  protected onConfirm(): void {
    this.languageConfirmed.emit(this.selectedLanguage());
  }

  protected onEscapeKey(event: Event): void {
    event.preventDefault();
    this.onConfirm();
  }
}
