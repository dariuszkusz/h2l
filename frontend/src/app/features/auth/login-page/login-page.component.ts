import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../../core/auth/store/auth.actions';
import {
  selectAuthError,
  selectAuthLoading,
  selectIsAuthenticated,
  selectSessionExpiredNotice,
} from '../../../core/auth/store/auth.selectors';
import { SupportedLanguage } from '../../../core/i18n/models/supported-language.type';
import { I18nActions } from '../../../core/i18n/store/i18n.actions';
import { selectCurrentLanguage } from '../../../core/i18n/store/i18n.selectors';
import { selectAlreadyInitializedRedirect } from '../../../core/setup/store/setup.selectors';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { HlmInputDirective } from '../../../shared/ui/hlm-input.directive';
import { HlmLabelDirective } from '../../../shared/ui/hlm-label.directive';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslatePipe,
    HlmButtonDirective,
    HlmCardDirective,
    HlmInputDirective,
    HlmLabelDirective,
    UiStateBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login-page.component.html',
})
export class LoginPageComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = this.store.selectSignal(selectAuthLoading);
  protected readonly authError = this.store.selectSignal(selectAuthError);
  protected readonly isAuthenticated = this.store.selectSignal(selectIsAuthenticated);
  protected readonly sessionExpiredNotice = this.store.selectSignal(selectSessionExpiredNotice);
  protected readonly alreadyInitializedRedirect = this.store.selectSignal(
    selectAlreadyInitializedRedirect,
  );
  protected readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);

  protected readonly showPassword = signal(false);
  protected readonly languages: readonly SupportedLanguage[] = ['en', 'de', 'pl'];

  protected readonly loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected togglePassword(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected changeLanguage(language: SupportedLanguage): void {
    this.store.dispatch(I18nActions.changeLanguage({ language }));
  }

  protected isControlInvalid(controlName: 'email' | 'password'): boolean {
    const control = this.loginForm.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  protected getEmailErrorKey(): string {
    const errors = this.loginForm.controls.email.errors;
    if (errors?.['required']) {
      return 'common.validation.required';
    }
    return 'common.validation.invalidEmail';
  }

  protected onSubmit(): void {
    this.store.dispatch(AuthActions.clearAuthError());

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const { email, password } = this.loginForm.getRawValue();
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    this.store.dispatch(
      AuthActions.login({
        request: { email: email.trim(), password },
        returnUrl,
      }),
    );
  }
}
