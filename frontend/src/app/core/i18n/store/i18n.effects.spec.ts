import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action, provideStore } from '@ngrx/store';
import { firstValueFrom, Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { LanguageStorageService } from '../services/language-storage.service';
import { I18nActions } from './i18n.actions';
import { I18nEffects } from './i18n.effects';
import { i18nReducer } from './i18n.reducer';

describe('I18nEffects', () => {
  let actions$: Observable<Action>;
  let effects: I18nEffects;
  let storage: LanguageStorageService;

  beforeEach(() => {
    globalThis.localStorage?.clear();
    TestBed.configureTestingModule({
      providers: [
        I18nEffects,
        provideStore({ i18n: i18nReducer }),
        provideMockActions(() => actions$),
      ],
    });
    effects = TestBed.inject(I18nEffects);
    storage = TestBed.inject(LanguageStorageService);
  });

  it('loads stored language on initialize', async () => {
    storage.setLanguage('default', 'de');
    actions$ = of(I18nActions.initialize());

    const result = await firstValueFrom(effects.initializeLanguage$);
    expect(result).toEqual(I18nActions.languageLoaded({ language: 'de' }));
  });

  it('loads independent language when switching session profile', async () => {
    storage.setLanguage('admin', 'pl');
    actions$ = of(I18nActions.switchSessionProfile({ sessionProfileId: 'admin' }));

    const result = await firstValueFrom(effects.reloadOnProfileSwitch$);
    expect(result).toEqual(I18nActions.languageLoaded({ language: 'pl' }));
  });
});
