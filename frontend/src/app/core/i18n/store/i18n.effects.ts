import { inject, Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { map, tap, withLatestFrom } from 'rxjs';
import { LanguageStorageService } from '../services/language-storage.service';
import { TranslationService } from '../services/translation.service';
import { I18nActions } from './i18n.actions';
import { selectActiveSessionProfileId } from './i18n.selectors';

@Injectable()
export class I18nEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store);
  private readonly languageStorage = inject(LanguageStorageService);
  private readonly translationService = inject(TranslationService);

  readonly initializeLanguage$ = createEffect(() =>
    this.actions$.pipe(
      ofType(I18nActions.initialize),
      withLatestFrom(this.store.select(selectActiveSessionProfileId)),
      map(([, sessionProfileId]) => {
        const stored = this.languageStorage.getLanguage(sessionProfileId);
        return I18nActions.languageLoaded({ language: stored });
      }),
    ),
  );

  readonly reloadOnProfileSwitch$ = createEffect(() =>
    this.actions$.pipe(
      ofType(I18nActions.switchSessionProfile),
      map(({ sessionProfileId }) => {
        const stored = this.languageStorage.getLanguage(sessionProfileId);
        return I18nActions.languageLoaded({ language: stored });
      }),
    ),
  );

  readonly persistAndApplyLanguage$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(I18nActions.selectFirstRunLanguage, I18nActions.changeLanguage),
        withLatestFrom(this.store.select(selectActiveSessionProfileId)),
        tap(([{ language }, sessionProfileId]) => {
          this.languageStorage.setLanguage(sessionProfileId, language);
          this.translationService.applyDocumentLanguage(language);
        }),
      ),
    { dispatch: false },
  );

  readonly applyLoadedLanguage$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(I18nActions.languageLoaded),
        tap(({ language }) => {
          this.translationService.applyDocumentLanguage(language ?? 'en');
        }),
      ),
    { dispatch: false },
  );
}
