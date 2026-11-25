import { describe, expect, it } from 'vitest';
import { selectColorPalette, selectThemeMode } from './theme.selectors';
import { ThemeState } from './theme.state';

describe('theme selectors', () => {
  const mockState: ThemeState = {
    mode: 'high-contrast',
    palette: 'forest',
  };

  it('selects theme mode', () => {
    expect(selectThemeMode.projector(mockState)).toBe('high-contrast');
  });

  it('selects color palette', () => {
    expect(selectColorPalette.projector(mockState)).toBe('forest');
  });
});
