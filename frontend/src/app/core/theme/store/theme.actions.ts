import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { ColorPalette } from '../models/color-palette.type';
import { ThemeMode } from '../models/theme-mode.type';

export const ThemeActions = createActionGroup({
  source: 'Theme',
  events: {
    Initialize: emptyProps(),
    'Theme Loaded': props<{ mode: ThemeMode; palette: ColorPalette }>(),
    'Set Theme Mode': props<{ mode: ThemeMode }>(),
    'Set Color Palette': props<{ palette: ColorPalette }>(),
  },
});
