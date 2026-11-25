import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { SupportedLanguage } from '../models/supported-language.type';

export const I18nActions = createActionGroup({
  source: 'I18n',
  events: {
    Initialize: emptyProps(),
    'Language Loaded': props<{ language: SupportedLanguage | null }>(),
    'Preview Language': props<{ language: SupportedLanguage }>(),
    'Select First Run Language': props<{ language: SupportedLanguage }>(),
    'Change Language': props<{ language: SupportedLanguage }>(),
    'Switch Session Profile': props<{ sessionProfileId: string }>(),
  },
});
