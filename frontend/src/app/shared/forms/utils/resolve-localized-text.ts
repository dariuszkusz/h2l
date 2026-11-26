import { components } from '../../../core/api/generated/openapi.types';

type LocalizedText = components['schemas']['LocalizedText'];
type SupportedLanguage = components['schemas']['SupportedLanguage'];

export function resolveLocalizedText(
  text: LocalizedText | undefined | null,
  language: SupportedLanguage,
  fallbackLanguage: SupportedLanguage = 'en',
): string {
  if (!text) {
    return '';
  }

  const direct = text[language]?.trim();
  if (direct && direct.length > 0) {
    return direct;
  }

  const fallback = text[fallbackLanguage]?.trim();
  if (fallback && fallback.length > 0) {
    return fallback;
  }

  const enFallback = text.en?.trim();
  if (enFallback && enFallback.length > 0) {
    return enFallback;
  }

  const anyValue = text.de?.trim() || text.pl?.trim();
  return anyValue ?? '';
}
