import { describe, expect, it } from 'vitest';
import { ThemeActions } from './theme.actions';
import { initialThemeState, themeReducer } from './theme.reducer';

describe('themeReducer', () => {
  it('returns initial state on unknown action', () => {
    expect(themeReducer(undefined, { type: 'UNKNOWN' })).toEqual(initialThemeState);
  });

  it('sets theme mode to dark and high-contrast', () => {
    const darkState = themeReducer(initialThemeState, ThemeActions.setThemeMode({ mode: 'dark' }));
    expect(darkState.mode).toBe('dark');

    const hcState = themeReducer(
      darkState,
      ThemeActions.setThemeMode({ mode: 'high-contrast' }),
    );
    expect(hcState.mode).toBe('high-contrast');
  });

  it('sets predefined color palette', () => {
    const state = themeReducer(
      initialThemeState,
      ThemeActions.setColorPalette({ palette: 'ocean' }),
    );
    expect(state.palette).toBe('ocean');
  });
});
