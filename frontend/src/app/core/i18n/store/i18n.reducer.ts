import { createReducer, on } from '@ngrx/store';
import { I18nActions } from './i18n.actions';
import { I18nState } from './i18n.state';

export const initialI18nState: I18nState = {
  language: 'en',
  hasSelectedLanguage: false,
  sessionProfileId: 'default',
};

export const i18nReducer = createReducer(
  initialI18nState,
  on(I18nActions.languageLoaded, (state, { language }): I18nState => ({
    ...state,
    language: language ?? state.language,
    hasSelectedLanguage: language !== null,
  })),
  on(I18nActions.previewLanguage, (state, { language }): I18nState => ({
    ...state,
    language,
  })),
  on(I18nActions.selectFirstRunLanguage, (state, { language }): I18nState => ({
    ...state,
    language,
    hasSelectedLanguage: true,
  })),
  on(I18nActions.changeLanguage, (state, { language }): I18nState => ({
    ...state,
    language,
    hasSelectedLanguage: true,
  })),
  on(I18nActions.switchSessionProfile, (state, { sessionProfileId }): I18nState => ({
    ...state,
    sessionProfileId,
  })),
);
