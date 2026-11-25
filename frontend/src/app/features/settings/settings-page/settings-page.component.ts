import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { SupportedLanguage } from '../../../core/i18n/models/supported-language.type';
import { I18nActions } from '../../../core/i18n/store/i18n.actions';
import {
  selectActiveSessionProfileId,
  selectCurrentLanguage,
} from '../../../core/i18n/store/i18n.selectors';
import { ColorPalette } from '../../../core/theme/models/color-palette.type';
import { ThemeMode } from '../../../core/theme/models/theme-mode.type';
import { ThemeActions } from '../../../core/theme/store/theme.actions';
import { selectColorPalette, selectThemeMode } from '../../../core/theme/store/theme.selectors';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [TranslatePipe, HlmButtonDirective, HlmCardDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="space-y-6">
      <header class="space-y-1">
        <h1 class="text-2xl font-semibold tracking-tight text-[var(--h2l-fg)]" data-testid="settings-heading">
          {{ 'settings.heading' | translate }}
        </h1>
        <p class="text-sm text-[var(--h2l-fg-muted)]">
          {{ 'settings.description' | translate }}
        </p>
      </header>

      <div hlmCard class="space-y-4">
        <div>
          <h2 class="text-base font-semibold text-[var(--h2l-fg)]">
            {{ 'settings.language.title' | translate }}
          </h2>
          <p class="text-sm text-[var(--h2l-fg-muted)]">
            {{ 'settings.language.description' | translate }}
          </p>
        </div>

        <div
          role="group"
          [attr.aria-label]="'settings.language.label' | translate"
          class="flex flex-wrap gap-3"
        >
          @for (lang of languages; track lang) {
            <button
              type="button"
              hlmBtn
              [variant]="currentLanguage() === lang ? 'primary' : 'outline'"
              [attr.aria-pressed]="currentLanguage() === lang"
              [attr.data-testid]="'settings-lang-' + lang"
              (click)="onLanguageChange(lang)"
            >
              {{ 'languages.' + lang | translate }}
            </button>
          }
        </div>
      </div>

      <div hlmCard class="space-y-4">
        <div>
          <h2 class="text-base font-semibold text-[var(--h2l-fg)]">
            {{ 'settings.theme.title' | translate }}
          </h2>
          <p class="text-sm text-[var(--h2l-fg-muted)]">
            {{ 'settings.theme.description' | translate }}
          </p>
        </div>

        <div
          role="group"
          [attr.aria-label]="'settings.theme.groupLabel' | translate"
          class="flex flex-wrap gap-3"
        >
          @for (mode of themeModes; track mode.value) {
            <button
              type="button"
              hlmBtn
              [variant]="themeMode() === mode.value ? 'primary' : 'outline'"
              [attr.aria-pressed]="themeMode() === mode.value"
              [attr.data-testid]="'theme-mode-' + mode.value"
              (click)="onThemeModeChange(mode.value)"
            >
              {{ mode.labelKey | translate }}
            </button>
          }
        </div>
      </div>

      <div hlmCard class="space-y-4">
        <div>
          <h2 class="text-base font-semibold text-[var(--h2l-fg)]">
            {{ 'settings.palette.title' | translate }}
          </h2>
          <p class="text-sm text-[var(--h2l-fg-muted)]">
            {{ 'settings.palette.description' | translate }}
          </p>
        </div>

        <div
          role="group"
          [attr.aria-label]="'settings.palette.groupLabel' | translate"
          class="flex flex-wrap gap-3"
        >
          @for (palette of palettes; track palette.value) {
            <button
              type="button"
              hlmBtn
              [variant]="colorPalette() === palette.value ? 'primary' : 'outline'"
              [attr.aria-pressed]="colorPalette() === palette.value"
              [attr.data-testid]="'palette-' + palette.value"
              (click)="onPaletteChange(palette.value)"
            >
              {{ palette.labelKey | translate }}
            </button>
          }
        </div>
      </div>

      <div hlmCard class="space-y-4">
        <div>
          <h2 class="text-base font-semibold text-[var(--h2l-fg)]">
            {{ 'settings.session.title' | translate }}
          </h2>
          <p class="text-sm text-[var(--h2l-fg-muted)]">
            {{ 'settings.session.description' | translate }}
          </p>
        </div>

        <div
          role="group"
          [attr.aria-label]="'settings.session.label' | translate"
          class="flex flex-wrap gap-3"
        >
          @for (profile of sessionProfiles; track profile.id) {
            <button
              type="button"
              hlmBtn
              [variant]="activeSessionProfileId() === profile.id ? 'primary' : 'outline'"
              [attr.aria-pressed]="activeSessionProfileId() === profile.id"
              [attr.data-testid]="'session-profile-' + profile.id"
              (click)="onSessionProfileSwitch(profile.id)"
            >
              {{ profile.labelKey | translate }}
            </button>
          }
        </div>
      </div>
    </section>
  `,
})
export class SettingsPageComponent {
  private readonly store = inject(Store);

  protected readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);
  protected readonly themeMode = this.store.selectSignal(selectThemeMode);
  protected readonly colorPalette = this.store.selectSignal(selectColorPalette);
  protected readonly activeSessionProfileId = this.store.selectSignal(selectActiveSessionProfileId);

  protected readonly languages: readonly SupportedLanguage[] = ['en', 'de', 'pl'];

  protected readonly themeModes: readonly { readonly value: ThemeMode; readonly labelKey: string }[] = [
    { value: 'light', labelKey: 'settings.theme.light' },
    { value: 'dark', labelKey: 'settings.theme.dark' },
    { value: 'high-contrast', labelKey: 'settings.theme.highContrast' },
  ];

  protected readonly palettes: readonly { readonly value: ColorPalette; readonly labelKey: string }[] = [
    { value: 'slate', labelKey: 'settings.palette.slate' },
    { value: 'ocean', labelKey: 'settings.palette.ocean' },
    { value: 'forest', labelKey: 'settings.palette.forest' },
  ];

  protected readonly sessionProfiles: readonly { readonly id: string; readonly labelKey: string }[] = [
    { id: 'default', labelKey: 'settings.session.defaultUser' },
    { id: 'admin', labelKey: 'settings.session.adminUser' },
    { id: 'end-user', labelKey: 'settings.session.endUser' },
  ];

  protected onLanguageChange(language: SupportedLanguage): void {
    this.store.dispatch(I18nActions.changeLanguage({ language }));
  }

  protected onThemeModeChange(mode: ThemeMode): void {
    this.store.dispatch(ThemeActions.setThemeMode({ mode }));
  }

  protected onPaletteChange(palette: ColorPalette): void {
    this.store.dispatch(ThemeActions.setColorPalette({ palette }));
  }

  protected onSessionProfileSwitch(sessionProfileId: string): void {
    this.store.dispatch(I18nActions.switchSessionProfile({ sessionProfileId }));
  }
}
