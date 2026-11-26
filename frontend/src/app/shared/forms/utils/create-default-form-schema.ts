import { components } from '../../../core/api/generated/openapi.types';

type FormSchema = components['schemas']['FormSchema'];

export function createDefaultFormSchema(): FormSchema {
  return {
    schemaVersion: '1.0',
    defaultLanguage: 'en',
    layoutMode: 'single',
    steps: [
      {
        id: 'step_1',
        title: {
          en: 'Step 1',
          de: 'Schritt 1',
          pl: 'Krok 1',
        },
        components: [],
      },
    ],
  };
}
