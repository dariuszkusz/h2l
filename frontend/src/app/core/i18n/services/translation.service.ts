import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { SupportedLanguage } from '../models/supported-language.type';
import { TranslationDictionary } from '../models/translation-dictionary.type';
import deTranslations from '../translations/de.json';
import enTranslations from '../translations/en.json';
import plTranslations from '../translations/pl.json';

@Injectable({
  providedIn: 'root',
})
export class TranslationService {
  private readonly document = inject(DOCUMENT);

  private readonly dictionaries: Record<SupportedLanguage, TranslationDictionary> = {
    en: enTranslations as TranslationDictionary,
    de: deTranslations as TranslationDictionary,
    pl: plTranslations as TranslationDictionary,
  };

  translate(
    key: string,
    language: SupportedLanguage,
    params?: Readonly<Record<string, string | number>>,
  ): string {
    const dictionary = this.dictionaries[language] ?? this.dictionaries.en;
    const resolved = this.resolveKey(dictionary, key) ?? this.resolveKey(this.dictionaries.en, key) ?? key;

    if (!params) {
      return resolved;
    }

    return Object.entries(params).reduce(
      (result, [paramKey, paramValue]) =>
        result.replaceAll(`{{${paramKey}}}`, String(paramValue)),
      resolved,
    );
  }

  applyDocumentLanguage(language: SupportedLanguage): void {
    this.document.documentElement.lang = language;
  }

  formatDate(date: Date, language: SupportedLanguage): string {
    return new Intl.DateTimeFormat(language, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(date);
  }

  formatNumber(value: number, language: SupportedLanguage): string {
    return new Intl.NumberFormat(language).format(value);
  }

  private resolveKey(dictionary: TranslationDictionary, key: string): string | null {
    const segments = key.split('.');
    let current: string | TranslationDictionary | undefined = dictionary;

    for (const segment of segments) {
      if (current === null || typeof current !== 'object') {
        return null;
      }
      current = current[segment];
    }

    return typeof current === 'string' ? current : null;
  }
}
