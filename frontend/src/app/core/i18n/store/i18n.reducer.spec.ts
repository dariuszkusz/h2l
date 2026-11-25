import { describe, expect, it } from 'vitest';
import { I18nActions } from './i18n.actions';
import { i18nReducer, initialI18nState } from './i18n.reducer';

describe('i18nReducer', () => {
  it('returns initial state on unknown action', () => {
    const state = i18nReducer(undefined, { type: 'UNKNOWN' });
    expect(state).toEqual(initialI18nState);
  });

  it('requires first-run language selection when no language is stored', () => {
    const state = i18nReducer(initialI18nState, I18nActions.languageLoaded({ language: null }));
    expect(state.hasSelectedLanguage).toBe(false);
    expect(state.language).toBe('en');
  });

  it('marks language as selected when loaded from storage', () => {
    const state = i18nReducer(initialI18nState, I18nActions.languageLoaded({ language: 'pl' }));
    expect(state.hasSelectedLanguage).toBe(true);
    expect(state.language).toBe('pl');
  });

  it('previews language without dismissing first-run dialog', () => {
    const state = i18nReducer(initialI18nState, I18nActions.previewLanguage({ language: 'de' }));
    expect(state.language).toBe('de');
    expect(state.hasSelectedLanguage).toBe(false);
  });

  it('confirms first-run language selection', () => {
    const state = i18nReducer(
      initialI18nState,
      I18nActions.selectFirstRunLanguage({ language: 'pl' }),
    );
    expect(state.language).toBe('pl');
    expect(state.hasSelectedLanguage).toBe(true);
  });

  it('switches active session profile independently', () => {
    const state = i18nReducer(
      initialI18nState,
      I18nActions.switchSessionProfile({ sessionProfileId: 'admin' }),
    );
    expect(state.sessionProfileId).toBe('admin');
  });
});
