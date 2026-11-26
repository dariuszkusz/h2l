import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../../core/auth/store/auth.actions';
import {
  selectCurrentUserRole,
  selectIsAuthenticated,
  selectPreferencesError,
  selectPreferencesSaved,
} from '../../../core/auth/store/auth.selectors';
import { SupportedLanguage } from '../../../core/i18n/models/supported-language.type';
import { I18nActions } from '../../../core/i18n/store/i18n.actions';
import {
  selectActiveSessionProfileId,
  selectCurrentLanguage,
} from '../../../core/i18n/store/i18n.selectors';
import { LicensingActions } from '../../../core/licensing/store/licensing.actions';
import {
  selectIsInGracePeriod,
  selectIsLicenseExpired,
  selectLicensingActivating,
  selectLicensingActivationSuccess,
  selectLicensingError,
  selectLicensingLoading,
  selectLicensingStatus,
} from '../../../core/licensing/store/licensing.selectors';
import { ColorPalette } from '../../../core/theme/models/color-palette.type';
import { ThemeMode } from '../../../core/theme/models/theme-mode.type';
import { ThemeActions } from '../../../core/theme/store/theme.actions';
import { selectColorPalette, selectThemeMode } from '../../../core/theme/store/theme.selectors';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmBadgeDirective } from '../../../shared/ui/hlm-badge.directive';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { HlmInputDirective } from '../../../shared/ui/hlm-input.directive';
import { HlmLabelDirective } from '../../../shared/ui/hlm-label.directive';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmBadgeDirective,
    HlmInputDirective,
    HlmLabelDirective,
    UiStateBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './settings-page.component.html',
})
export class SettingsPageComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly fb = inject(FormBuilder);

  protected readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);
  protected readonly themeMode = this.store.selectSignal(selectThemeMode);
  protected readonly colorPalette = this.store.selectSignal(selectColorPalette);
  protected readonly activeSessionProfileId = this.store.selectSignal(selectActiveSessionProfileId);
  protected readonly isAuthenticated = this.store.selectSignal(selectIsAuthenticated);
  protected readonly currentUserRole = this.store.selectSignal(selectCurrentUserRole);
  protected readonly preferencesSaved = this.store.selectSignal(selectPreferencesSaved);
  protected readonly preferencesError = this.store.selectSignal(selectPreferencesError);

  protected readonly licensingStatus = this.store.selectSignal(selectLicensingStatus);
  protected readonly licensingLoading = this.store.selectSignal(selectLicensingLoading);
  protected readonly licensingActivating = this.store.selectSignal(selectLicensingActivating);
  protected readonly licensingError = this.store.selectSignal(selectLicensingError);
  protected readonly licensingActivationSuccess = this.store.selectSignal(
    selectLicensingActivationSuccess,
  );
  protected readonly isInGracePeriod = this.store.selectSignal(selectIsInGracePeriod);
  protected readonly isLicenseExpired = this.store.selectSignal(selectIsLicenseExpired);

  protected readonly isAdmin = computed(() => this.currentUserRole() === 'Admin');
  protected readonly copiedInstanceId = signal(false);

  protected readonly licenseForm = this.fb.nonNullable.group({
    licenseKey: ['', [Validators.required]],
  });

  protected readonly languages: readonly SupportedLanguage[] = ['en', 'de', 'pl'];

  protected readonly themeModes: readonly {
    readonly value: ThemeMode;
    readonly labelKey: string;
  }[] = [
    { value: 'light', labelKey: 'settings.theme.light' },
    { value: 'dark', labelKey: 'settings.theme.dark' },
    { value: 'high-contrast', labelKey: 'settings.theme.highContrast' },
  ];

  protected readonly palettes: readonly {
    readonly value: ColorPalette;
    readonly labelKey: string;
  }[] = [
    { value: 'slate', labelKey: 'settings.palette.slate' },
    { value: 'ocean', labelKey: 'settings.palette.ocean' },
    { value: 'forest', labelKey: 'settings.palette.forest' },
  ];

  protected readonly sessionProfiles: readonly {
    readonly id: string;
    readonly labelKey: string;
  }[] = [
    { id: 'default', labelKey: 'settings.session.defaultUser' },
    { id: 'admin', labelKey: 'settings.session.adminUser' },
    { id: 'end-user', labelKey: 'settings.session.endUser' },
  ];

  ngOnInit(): void {
    this.store.dispatch(AuthActions.clearPreferencesFeedback());
    if (this.isAdmin()) {
      this.store.dispatch(LicensingActions.clearFeedback());
      this.store.dispatch(LicensingActions.loadStatus());
    }
  }

  protected onLanguageChange(language: SupportedLanguage): void {
    this.store.dispatch(I18nActions.changeLanguage({ language }));
    if (this.isAuthenticated()) {
      this.store.dispatch(
        AuthActions.updatePreferences({
          request: { preferredLanguage: language },
        }),
      );
    }
  }

  protected onThemeModeChange(mode: ThemeMode): void {
    this.store.dispatch(ThemeActions.setThemeMode({ mode }));
    if (this.isAuthenticated()) {
      this.store.dispatch(
        AuthActions.updatePreferences({
          request: { preferredTheme: mode },
        }),
      );
    }
  }

  protected onPaletteChange(palette: ColorPalette): void {
    this.store.dispatch(ThemeActions.setColorPalette({ palette }));
  }

  protected onSessionProfileSwitch(sessionProfileId: string): void {
    this.store.dispatch(I18nActions.switchSessionProfile({ sessionProfileId }));
  }

  protected onCopyInstanceId(instanceId: string): void {
    if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
      void navigator.clipboard.writeText(instanceId).then(() => {
        this.copiedInstanceId.set(true);
      });
    } else {
      this.copiedInstanceId.set(true);
    }
  }

  protected onActivateLicenseSubmit(): void {
    if (this.licenseForm.invalid) {
      this.licenseForm.markAllAsTouched();
      return;
    }

    const licenseKey = this.licenseForm.getRawValue().licenseKey.trim();
    if (licenseKey.length === 0) {
      this.licenseForm.controls.licenseKey.setErrors({ required: true });
      return;
    }

    this.store.dispatch(LicensingActions.activateLicense({ request: { licenseKey } }));
  }
}
