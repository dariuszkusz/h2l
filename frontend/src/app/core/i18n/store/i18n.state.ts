import { SupportedLanguage } from '../models/supported-language.type';

export interface I18nState {
  readonly language: SupportedLanguage;
  readonly hasSelectedLanguage: boolean;
  readonly sessionProfileId: string;
}
