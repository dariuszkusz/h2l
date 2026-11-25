import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { ThemeDomService } from './theme-dom.service';

describe('ThemeDomService', () => {
  let service: ThemeDomService;
  let doc: Document;

  beforeEach(() => {
    globalThis.localStorage?.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(ThemeDomService);
    doc = TestBed.inject(DOCUMENT);
  });

  it('applies data-theme, data-palette, and dark class on documentElement', () => {
    service.applyTheme('dark', 'ocean');
    expect(doc.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(doc.documentElement.getAttribute('data-palette')).toBe('ocean');
    expect(doc.documentElement.classList.contains('dark')).toBe(true);

    service.applyTheme('high-contrast', 'forest');
    expect(doc.documentElement.getAttribute('data-theme')).toBe('high-contrast');
    expect(doc.documentElement.getAttribute('data-palette')).toBe('forest');
    expect(doc.documentElement.classList.contains('dark')).toBe(false);
  });

  it('persists and restores theme mode and palette', () => {
    service.persistThemeMode('high-contrast');
    service.persistColorPalette('ocean');
    expect(service.getInitialThemeMode()).toBe('high-contrast');
    expect(service.getInitialColorPalette()).toBe('ocean');
  });
});
