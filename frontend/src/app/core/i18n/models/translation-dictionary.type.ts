export interface TranslationDictionary {
  readonly [key: string]: string | TranslationDictionary;
}
