import { describe, expect, it } from 'vitest';
import {
  selectActiveSessionProfileId,
  selectCurrentLanguage,
  selectIsFirstRunLanguageSelectionRequired,
} from './i18n.selectors';
import { I18nState } from './i18n.state';

describe('i18n selectors', () => {
  const mockState: I18nState = {
    language: 'de',
    hasSelectedLanguage: false,
    sessionProfileId: 'end-user',
  };

  it('selects current language', () => {
    expect(selectCurrentLanguage.projector(mockState)).toBe('de');
  });

  it('selects whether first-run language selection is required', () => {
    expect(selectIsFirstRunLanguageSelectionRequired.projector(mockState)).toBe(true);
    expect(
      selectIsFirstRunLanguageSelectionRequired.projector({
        ...mockState,
        hasSelectedLanguage: true,
      }),
    ).toBe(false);
  });

  it('selects active session profile id', () => {
    expect(selectActiveSessionProfileId.projector(mockState)).toBe('end-user');
  });
});
