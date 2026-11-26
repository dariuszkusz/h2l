import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Store } from '@ngrx/store';
import { AuthActions } from './core/auth/store/auth.actions';
import {
  selectCurrentUser,
  selectCurrentUserRole,
  selectForbiddenError,
  selectIsAuthenticated,
} from './core/auth/store/auth.selectors';
import { ConnectivityActions } from './core/connectivity/store/connectivity.actions';
import {
  selectIsOnline,
  selectPendingDraftsCount,
  selectSyncStatus,
} from './core/connectivity/store/connectivity.selectors';
import { SupportedLanguage } from './core/i18n/models/supported-language.type';
import { I18nActions } from './core/i18n/store/i18n.actions';
import { selectIsFirstRunLanguageSelectionRequired } from './core/i18n/store/i18n.selectors';
import { SetupActions } from './core/setup/store/setup.actions';
import { selectIsSetupInitialized } from './core/setup/store/setup.selectors';
import { ThemeActions } from './core/theme/store/theme.actions';
import { FirstRunLanguageDialogComponent } from './features/onboarding/first-run-language-dialog/first-run-language-dialog.component';
import { BrandLogoComponent } from './shared/components/brand-logo/brand-logo.component';
import { ConnectivityBannerComponent } from './shared/components/connectivity-banner/connectivity-banner.component';
import { UiStateBannerComponent } from './shared/components/ui-state-banner/ui-state-banner.component';
import { TranslatePipe } from './shared/pipes/translate.pipe';
import { HlmBadgeDirective } from './shared/ui/hlm-badge.directive';
import { HlmButtonDirective } from './shared/ui/hlm-button.directive';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslatePipe,
    HlmButtonDirective,
    HlmBadgeDirective,
    BrandLogoComponent,
    ConnectivityBannerComponent,
    FirstRunLanguageDialogComponent,
    UiStateBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly store = inject(Store);

  protected readonly isFirstRunLanguageRequired = this.store.selectSignal(
    selectIsFirstRunLanguageSelectionRequired,
  );
  protected readonly isOnline = this.store.selectSignal(selectIsOnline);
  protected readonly pendingDraftsCount = this.store.selectSignal(selectPendingDraftsCount);
  protected readonly syncStatus = this.store.selectSignal(selectSyncStatus);
  protected readonly isAuthenticated = this.store.selectSignal(selectIsAuthenticated);
  protected readonly currentUser = this.store.selectSignal(selectCurrentUser);
  protected readonly currentUserRole = this.store.selectSignal(selectCurrentUserRole);
  protected readonly isAdmin = computed(() => this.currentUserRole() === 'Admin');
  protected readonly canManageProjects = computed(() => {
    const role = this.currentUserRole();
    return role === 'Admin' || role === 'ProcessDesigner';
  });
  protected readonly forbiddenError = this.store.selectSignal(selectForbiddenError);
  protected readonly isSetupInitialized = this.store.selectSignal(selectIsSetupInitialized);
  protected readonly previewLanguage = signal<SupportedLanguage>('en');

  ngOnInit(): void {
    this.store.dispatch(I18nActions.initialize());
    this.store.dispatch(ThemeActions.initialize());
    this.store.dispatch(ConnectivityActions.initialize());
    this.store.dispatch(SetupActions.checkStatus());
    this.store.dispatch(AuthActions.hydrateSession());
  }

  protected onFirstRunPreview(language: SupportedLanguage): void {
    this.previewLanguage.set(language);
    this.store.dispatch(I18nActions.previewLanguage({ language }));
  }

  protected onFirstRunConfirm(language: SupportedLanguage): void {
    this.store.dispatch(I18nActions.selectFirstRunLanguage({ language }));
  }

  protected onLogout(): void {
    this.store.dispatch(AuthActions.logout());
  }

  protected onDismissForbidden(): void {
    this.store.dispatch(AuthActions.clearAuthError());
  }
}
