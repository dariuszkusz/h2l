import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from '../../../core/auth/store/auth.actions';
import {
  selectActivationError,
  selectActivationStatus,
} from '../../../core/auth/store/auth.selectors';
import { UiStateBannerComponent } from '../../../shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { HlmButtonDirective } from '../../../shared/ui/hlm-button.directive';
import { HlmCardDirective } from '../../../shared/ui/hlm-card.directive';
import { HlmInputDirective } from '../../../shared/ui/hlm-input.directive';
import { HlmLabelDirective } from '../../../shared/ui/hlm-label.directive';

function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value as string | undefined;
  const confirmPassword = group.get('confirmPassword')?.value as string | undefined;

  if (!confirmPassword) {
    return null;
  }

  return password === confirmPassword ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-activate-page',
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
  templateUrl: './activate-page.component.html',
})
export class ActivatePageComponent implements OnInit, OnDestroy {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly activationStatus = this.store.selectSignal(selectActivationStatus);
  protected readonly activationError = this.store.selectSignal(selectActivationError);

  protected readonly showPassword = signal(false);
  protected readonly showConfirmPassword = signal(false);
  private readonly tokenSignal = signal('');

  protected readonly hasTokenValue = computed(() => this.tokenSignal().trim().length > 0);

  protected readonly activateForm = this.fb.group(
    {
      token: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(12), Validators.maxLength(128)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator },
  );

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const token = params.get('token')?.trim() ?? '';
      if (token.length > 0) {
        this.activateForm.controls.token.setValue(token);
        this.tokenSignal.set(token);
      }
    });

    this.activateForm.controls.token.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.tokenSignal.set(value);
      });
  }

  ngOnDestroy(): void {
    this.store.dispatch(AuthActions.resetActivationState());
  }

  protected togglePassword(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected toggleConfirmPassword(): void {
    this.showConfirmPassword.update((visible) => !visible);
  }

  protected isControlInvalid(controlName: 'token' | 'password'): boolean {
    const control = this.activateForm.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  protected isConfirmPasswordInvalid(): boolean {
    const confirmControl = this.activateForm.controls.confirmPassword;
    const interacted = confirmControl.touched || confirmControl.dirty;
    if (!interacted) {
      return false;
    }

    return confirmControl.invalid || this.activateForm.hasError('passwordMismatch');
  }

  protected getPasswordErrorKey(): string {
    const errors = this.activateForm.controls.password.errors;
    if (errors?.['required']) {
      return 'common.validation.required';
    }
    if (errors?.['maxlength']) {
      return 'common.validation.maxLength';
    }
    return 'common.validation.minLength';
  }

  protected getConfirmPasswordErrorKey(): string {
    const confirmErrors = this.activateForm.controls.confirmPassword.errors;
    if (confirmErrors?.['required']) {
      return 'common.validation.required';
    }
    return 'common.validation.passwordMismatch';
  }

  protected onSubmit(): void {
    if (this.activateForm.invalid) {
      this.activateForm.markAllAsTouched();
      return;
    }

    const { token, password } = this.activateForm.getRawValue();
    this.store.dispatch(
      AuthActions.activateAccount({
        request: {
          token: token.trim(),
          password,
        },
      }),
    );
  }
}
