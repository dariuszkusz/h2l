import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { ColorPalette } from '../models/color-palette.type';
import { ThemeMode } from '../models/theme-mode.type';

const THEME_STORAGE_KEY = 'h2l.theme.mode';
const PALETTE_STORAGE_KEY = 'h2l.theme.palette';
const VALID_MODES: readonly ThemeMode[] = ['light', 'dark', 'high-contrast'];
const VALID_PALETTES: readonly ColorPalette[] = ['slate', 'ocean', 'forest'];

@Injectable({
  providedIn: 'root',
})
export class ThemeDomService {
  private readonly document = inject(DOCUMENT);

  getInitialThemeMode(): ThemeMode {
    const stored = this.readStoredThemeMode();
    if (stored) {
      return stored;
    }
    return this.prefersDarkColorScheme() ? 'dark' : 'light';
  }

  getInitialColorPalette(): ColorPalette {
    try {
      const value = globalThis.localStorage?.getItem(PALETTE_STORAGE_KEY);
      if (value && (VALID_PALETTES as readonly string[]).includes(value)) {
        return value as ColorPalette;
      }
      return 'slate';
    } catch {
      return 'slate';
    }
  }

  applyTheme(mode: ThemeMode, palette: ColorPalette): void {
    const root = this.document.documentElement;
    root.setAttribute('data-theme', mode);
    root.setAttribute('data-palette', palette);
    root.classList.toggle('dark', mode === 'dark');
  }

  persistThemeMode(mode: ThemeMode): void {
    try {
      globalThis.localStorage?.setItem(THEME_STORAGE_KEY, mode);
    } catch {
      // Storage unavailable
    }
  }

  persistColorPalette(palette: ColorPalette): void {
    try {
      globalThis.localStorage?.setItem(PALETTE_STORAGE_KEY, palette);
    } catch {
      // Storage unavailable
    }
  }

  private readStoredThemeMode(): ThemeMode | null {
    try {
      const value = globalThis.localStorage?.getItem(THEME_STORAGE_KEY);
      if (value && (VALID_MODES as readonly string[]).includes(value)) {
        return value as ThemeMode;
      }
      return null;
    } catch {
      return null;
    }
  }

  private prefersDarkColorScheme(): boolean {
    try {
      return globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
    } catch {
      return false;
    }
  }
}
