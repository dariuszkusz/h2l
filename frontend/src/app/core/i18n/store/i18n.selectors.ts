import { createFeatureSelector, createSelector } from '@ngrx/store';
import { I18nState } from './i18n.state';

export const selectI18nState = createFeatureSelector<I18nState>('i18n');

export const selectCurrentLanguage = createSelector(
  selectI18nState,
  (state) => state.language,
);

export const selectIsFirstRunLanguageSelectionRequired = createSelector(
  selectI18nState,
  (state) => !state.hasSelectedLanguage,
);

export const selectActiveSessionProfileId = createSelector(
  selectI18nState,
  (state) => state.sessionProfileId,
);
