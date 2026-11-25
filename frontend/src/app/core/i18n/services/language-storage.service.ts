import { Injectable } from '@angular/core';
import { SupportedLanguage } from '../models/supported-language.type';

const STORAGE_KEY_PREFIX = 'h2l.lang.';
const VALID_LANGUAGES: readonly SupportedLanguage[] = ['en', 'de', 'pl'];

@Injectable({
  providedIn: 'root',
})
export class LanguageStorageService {
  getLanguage(sessionProfileId: string): SupportedLanguage | null {
    try {
      const value = globalThis.localStorage?.getItem(`${STORAGE_KEY_PREFIX}${sessionProfileId}`);
      if (value && this.isSupportedLanguage(value)) {
        return value;
      }
      return null;
    } catch {
      return null;
    }
  }

  setLanguage(sessionProfileId: string, language: SupportedLanguage): void {
    try {
      globalThis.localStorage?.setItem(`${STORAGE_KEY_PREFIX}${sessionProfileId}`, language);
    } catch {
      // Storage unavailable (e.g. private browsing restrictions)
    }
  }

  private isSupportedLanguage(value: string): value is SupportedLanguage {
    return (VALID_LANGUAGES as readonly string[]).includes(value);
  }
}
