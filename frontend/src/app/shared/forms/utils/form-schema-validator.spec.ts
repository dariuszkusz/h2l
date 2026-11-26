import { describe, expect, it } from 'vitest';
import { components } from '../../../core/api/generated/openapi.types';
import { isValidVariableKey, validateFormSchema } from './form-schema-validator';
import { resolveLocalizedText } from './resolve-localized-text';

type FormSchema = components['schemas']['FormSchema'];

describe('form-schema-validator and resolveLocalizedText', () => {
  it('validates variableKey format and uniqueness', () => {
    expect(isValidVariableKey('softwareName_1')).toBe(true);
    expect(isValidVariableKey('1invalid')).toBe(false);
    expect(isValidVariableKey('has-dash')).toBe(false);

    const invalidSchema: FormSchema = {
      schemaVersion: '1.0',
      defaultLanguage: 'en',
      layoutMode: 'single',
      steps: [
        {
          id: 'step_1',
          title: { en: 'Step 1' },
          components: [
            { id: 'c1', type: 'text', variableKey: 'amount' },
            { id: 'c2', type: 'number', variableKey: 'amount' },
            { id: 'c3', type: 'select', variableKey: 'choice', options: [] },
            {
              id: 'c4',
              type: 'text',
              variableKey: 'note',
              rules: { visibleIf: 'bad ===' },
            },
          ],
        },
      ],
    };

    const issues = validateFormSchema(invalidSchema);
    expect(issues.some((i) => i.code === 'VARIABLE_KEY_DUPLICATE')).toBe(true);
    expect(issues.some((i) => i.code === 'OPTIONS_REQUIRED')).toBe(true);
    expect(issues.some((i) => i.code === 'INVALID_EXPRESSION')).toBe(true);
  });

  it('resolves localized text with fallback to defaultLanguage and en', () => {
    expect(resolveLocalizedText({ en: 'Cost', de: 'Kosten', pl: 'Koszt' }, 'pl', 'en')).toBe('Koszt');
    expect(resolveLocalizedText({ en: 'Cost' }, 'de', 'en')).toBe('Cost');
    expect(resolveLocalizedText(undefined, 'en', 'en')).toBe('');
  });
});
