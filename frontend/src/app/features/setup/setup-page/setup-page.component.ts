import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { SupportedLanguage } from '../../../core/i18n/models/supported-language.type';
import { I18nActions } from '../../../core/i18n/store/i18n.actions';
import { selectCurrentLanguage } from '../../../core/i18n/store/i18n.selectors';
import { SetupActions } from '../../../core/setup/store/setup.actions';
import {
  selectSetupError,
  selectSetupInitializedSuccess,
  selectSetupLoadingStatus,
  selectSetupSubmitting,
} from '../../../core/setup/store/setup.selectors';
import { ColorPalette } from '../../../core/theme/models/color-palette.type';
import { ThemeMode } from '../../../core/theme/models/theme-mode.type';
import { ThemeActions } from '../../../core/theme/store/theme.actions';
import { selectColorPalette, selectThemeMode } from '../../../core/theme/store/theme.selectors';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { HlmInputDirective } from '../../../shared/ui/hlm-input.directive';
import { HlmLabelDirective } from '../../../shared/ui/hlm-label.directive';

@Component({
  selector: 'app-setup-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmInputDirective,
    HlmLabelDirective,
    UiStateBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './setup-page.component.html',
})
export class SetupPageComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly store = inject(Store);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly loadingStatus = this.store.selectSignal(selectSetupLoadingStatus);
  protected readonly submitting = this.store.selectSignal(selectSetupSubmitting);
  protected readonly setupError = this.store.selectSignal(selectSetupError);
  protected readonly initializedSuccess = this.store.selectSignal(selectSetupInitializedSuccess);
  protected readonly currentThemeMode = this.store.selectSignal(selectThemeMode);
  private readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);
  private readonly currentPalette = this.store.selectSignal(selectColorPalette);

  protected readonly showAdminPassword = signal(false);
  protected readonly showSmtpPassword = signal(false);

  protected readonly languages: readonly SupportedLanguage[] = ['en', 'de', 'pl'];
  protected readonly palettes: readonly { readonly value: ColorPalette; readonly labelKey: string }[] = [
    { value: 'slate', labelKey: 'settings.palette.slate' },
    { value: 'ocean', labelKey: 'settings.palette.ocean' },
    { value: 'forest', labelKey: 'settings.palette.forest' },
  ];
  protected readonly themeModes: readonly { readonly value: ThemeMode; readonly labelKey: string }[] = [
    { value: 'light', labelKey: 'settings.theme.light' },
    { value: 'dark', labelKey: 'settings.theme.dark' },
    { value: 'high-contrast', labelKey: 'settings.theme.highContrast' },
  ];

  protected readonly setupForm = this.fb.group({
    adminEmail: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
    adminPassword: ['', [Validators.required, Validators.minLength(12), Validators.maxLength(128)]],
    defaultLanguage: this.fb.control<SupportedLanguage>(this.currentLanguage()),
    selectedPalette: this.fb.control<ColorPalette>(this.currentPalette()),
    enableSmtp: false,
    smtp: this.fb.group({
      host: ['', [Validators.maxLength(255)]],
      port: [587],
      useSsl: true,
      username: ['', [Validators.maxLength(255)]],
      password: ['', [Validators.maxLength(512)]],
      fromAddress: ['', [Validators.maxLength(320)]],
      fromName: ['', [Validators.maxLength(200)]],
    }),
  });

  ngOnInit(): void {
    this.setupForm.controls.enableSmtp.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((enabled) => {
        this.applySmtpValidators(enabled);
      });
  }

  protected toggleAdminPassword(): void {
    this.showAdminPassword.update((visible) => !visible);
  }

  protected toggleSmtpPassword(): void {
    this.showSmtpPassword.update((visible) => !visible);
  }

  protected selectLanguage(language: SupportedLanguage): void {
    this.setupForm.controls.defaultLanguage.setValue(language);
    this.store.dispatch(I18nActions.changeLanguage({ language }));
  }

  protected selectPalette(palette: ColorPalette): void {
    this.setupForm.controls.selectedPalette.setValue(palette);
    this.store.dispatch(ThemeActions.setColorPalette({ palette }));
  }

  protected selectPreviewMode(mode: ThemeMode): void {
    this.store.dispatch(ThemeActions.setThemeMode({ mode }));
  }

  protected retryStatus(): void {
    this.store.dispatch(SetupActions.checkStatus());
  }

  protected isControlInvalid(controlName: 'adminEmail' | 'adminPassword'): boolean {
    const control = this.setupForm.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  protected isSmtpControlInvalid(controlName: 'host' | 'port' | 'fromAddress'): boolean {
    const control = this.setupForm.controls.smtp.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  protected getEmailErrorKey(): string {
    const errors = this.setupForm.controls.adminEmail.errors;
    if (errors?.['required']) {
      return 'common.validation.required';
    }
    if (errors?.['maxlength']) {
      return 'common.validation.maxLength';
    }
    return 'common.validation.invalidEmail';
  }

  protected getPasswordErrorKey(): string {
    const errors = this.setupForm.controls.adminPassword.errors;
    if (errors?.['required']) {
      return 'common.validation.required';
    }
    if (errors?.['maxlength']) {
      return 'common.validation.maxLength';
    }
    return 'common.validation.minLength';
  }

  protected getSmtpFromErrorKey(): string {
    const errors = this.setupForm.controls.smtp.controls.fromAddress.errors;
    if (errors?.['required']) {
      return 'common.validation.required';
    }
    return 'common.validation.invalidEmail';
  }

  protected onSubmit(): void {
    this.store.dispatch(SetupActions.clearSetupError());

    if (this.setupForm.invalid) {
      this.setupForm.markAllAsTouched();
      return;
    }

    const raw = this.setupForm.getRawValue();
    const request: components['schemas']['InitializeSetupRequest'] = {
      adminEmail: raw.adminEmail.trim(),
      adminPassword: raw.adminPassword,
      defaultLanguage: raw.defaultLanguage,
      selectedPalette: raw.selectedPalette,
      smtp: raw.enableSmtp
        ? {
            host: raw.smtp.host.trim(),
            port: raw.smtp.port,
            useSsl: raw.smtp.useSsl,
            username: this.normalizeOptionalString(raw.smtp.username),
            password: raw.smtp.password.length > 0 ? raw.smtp.password : null,
            fromAddress: raw.smtp.fromAddress.trim(),
            fromName: this.normalizeOptionalString(raw.smtp.fromName),
          }
        : null,
    };

    this.store.dispatch(SetupActions.initialize({ request }));
  }

  private applySmtpValidators(enabled: boolean): void {
    const smtpControls = this.setupForm.controls.smtp.controls;

    if (enabled) {
      smtpControls.host.setValidators([Validators.required, Validators.maxLength(255)]);
      smtpControls.port.setValidators([
        Validators.required,
        Validators.min(1),
        Validators.max(65535),
      ]);
      smtpControls.fromAddress.setValidators([
        Validators.required,
        Validators.email,
        Validators.maxLength(320),
      ]);
    } else {
      smtpControls.host.clearValidators();
      smtpControls.port.clearValidators();
      smtpControls.fromAddress.clearValidators();
    }

    smtpControls.host.updateValueAndValidity();
    smtpControls.port.updateValueAndValidity();
    smtpControls.fromAddress.updateValueAndValidity();
  }

  private normalizeOptionalString(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
}
