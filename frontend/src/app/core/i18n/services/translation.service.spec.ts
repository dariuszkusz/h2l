import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import deTranslations from '../translations/de.json';
import enTranslations from '../translations/en.json';
import plTranslations from '../translations/pl.json';
import { TranslationService } from './translation.service';

function flattenKeys(obj: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(obj).flatMap(([key, value]) => {
    const full = prefix ? `${prefix}.${key}` : key;
    if (value !== null && typeof value === 'object' && !Array.isArray(value)) {
      return flattenKeys(value as Record<string, unknown>, full);
    }
    return [full];
  });
}

describe('TranslationService', () => {
  let service: TranslationService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TranslationService);
  });

  it('translates keys in en, de (du), and pl (Ty)', () => {
    expect(service.translate('onboarding.languageDialog.title', 'en')).toBe('Choose your language');
    expect(service.translate('onboarding.languageDialog.title', 'de')).toBe('Wähle deine Sprache');
    expect(service.translate('onboarding.languageDialog.title', 'pl')).toBe('Wybierz swój język');
  });

  it('interpolates parameters in translated strings', () => {
    expect(service.translate('connectivity.queued', 'en', { count: 4 })).toBe('Pending drafts: 4');
    expect(service.translate('connectivity.queued', 'pl', { count: 4 })).toBe(
      'Oczekujące wersje robocze: 4',
    );
  });

  it('falls back to key when translation does not exist', () => {
    expect(service.translate('missing.translation.key', 'en')).toBe('missing.translation.key');
  });

  it('maintains exact key parity across en, de, and pl dictionaries', () => {
    const enKeys = flattenKeys(enTranslations).sort();
    const deKeys = flattenKeys(deTranslations).sort();
    const plKeys = flattenKeys(plTranslations).sort();

    expect(deKeys).toEqual(enKeys);
    expect(plKeys).toEqual(enKeys);
  });
});
