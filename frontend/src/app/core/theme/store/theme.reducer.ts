import { createReducer, on } from '@ngrx/store';
import { ThemeActions } from './theme.actions';
import { ThemeState } from './theme.state';

export const initialThemeState: ThemeState = {
  mode: 'light',
  palette: 'slate',
};

export const themeReducer = createReducer(
  initialThemeState,
  on(ThemeActions.themeLoaded, (_state, { mode, palette }): ThemeState => ({
    mode,
    palette,
  })),
  on(ThemeActions.setThemeMode, (state, { mode }): ThemeState => ({
    ...state,
    mode,
  })),
  on(ThemeActions.setColorPalette, (state, { palette }): ThemeState => ({
    ...state,
    palette,
  })),
);
