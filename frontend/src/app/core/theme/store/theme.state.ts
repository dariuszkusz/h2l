import { ColorPalette } from '../models/color-palette.type';
import { ThemeMode } from '../models/theme-mode.type';

export interface ThemeState {
  readonly mode: ThemeMode;
  readonly palette: ColorPalette;
}
