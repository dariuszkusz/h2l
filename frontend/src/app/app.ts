import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Store } from '@ngrx/store';
import { ConnectivityActions } from './core/connectivity/store/connectivity.actions';
import {
  selectIsOnline,
  selectPendingDraftsCount,
  selectSyncStatus,
} from './core/connectivity/store/connectivity.selectors';
import { SupportedLanguage } from './core/i18n/models/supported-language.type';
import { I18nActions } from './core/i18n/store/i18n.actions';
import {
  selectCurrentLanguage,
  selectIsFirstRunLanguageSelectionRequired,
} from './core/i18n/store/i18n.selectors';
import { ThemeActions } from './core/theme/store/theme.actions';
import { FirstRunLanguageDialogComponent } from './features/onboarding/first-run-language-dialog/first-run-language-dialog.component';
import { ConnectivityBannerComponent } from './shared/components/connectivity-banner/connectivity-banner.component';
import { TranslatePipe } from './shared/pipes/translate.pipe';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslatePipe,
    ConnectivityBannerComponent,
    FirstRunLanguageDialogComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly store = inject(Store);

  protected readonly currentLanguage = this.store.selectSignal(selectCurrentLanguage);
  protected readonly isFirstRunLanguageRequired = this.store.selectSignal(
    selectIsFirstRunLanguageSelectionRequired,
  );
  protected readonly isOnline = this.store.selectSignal(selectIsOnline);
  protected readonly pendingDraftsCount = this.store.selectSignal(selectPendingDraftsCount);
  protected readonly syncStatus = this.store.selectSignal(selectSyncStatus);
  protected readonly previewLanguage = signal<SupportedLanguage>('en');

  ngOnInit(): void {
    this.store.dispatch(I18nActions.initialize());
    this.store.dispatch(ThemeActions.initialize());
    this.store.dispatch(ConnectivityActions.initialize());
  }

  protected onFirstRunPreview(language: SupportedLanguage): void {
    this.previewLanguage.set(language);
    this.store.dispatch(I18nActions.previewLanguage({ language }));
  }

  protected onFirstRunConfirm(language: SupportedLanguage): void {
    this.store.dispatch(I18nActions.selectFirstRunLanguage({ language }));
  }
}
