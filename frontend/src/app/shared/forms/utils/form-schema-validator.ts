import { components } from '../../../core/api/generated/openapi.types';
import { validateExpressionSyntax } from '../ast/safe-ast-evaluator';
import { SchemaValidationIssue } from './schema-validation-issue.model';

type FormSchema = components['schemas']['FormSchema'];
type FormComponent = components['schemas']['FormComponent'];

export const VARIABLE_KEY_PATTERN = /^[a-zA-Z][a-zA-Z0-9_]{0,63}$/;

const NON_DATA_TYPES: ReadonlySet<string> = new Set([
  'section',
  'grid',
  'heading',
  'text_block',
  'divider',
]);

const OPTION_TYPES: ReadonlySet<string> = new Set([
  'select',
  'radio',
  'checkbox_group',
]);

export function isValidVariableKey(key: string | undefined | null): boolean {
  if (!key) {
    return false;
  }
  return VARIABLE_KEY_PATTERN.test(key);
}

export function validateFormSchema(schema: FormSchema): readonly SchemaValidationIssue[] {
  const issues: SchemaValidationIssue[] = [];

  if (!schema.steps || schema.steps.length === 0) {
    issues.push({
      componentId: null,
      code: 'EMPTY_STEPS',
      message: 'Form must contain at least one step.',
    });
    return issues;
  }

  const seenVariableKeys = new Set<string>();

  const visitComponent = (component: FormComponent): void => {
    const componentType = component.type as string;
    const collectsData = !NON_DATA_TYPES.has(componentType);

    if (collectsData) {
      const key = component.variableKey?.trim() ?? '';
      if (key.length === 0) {
        issues.push({
          componentId: component.id,
          code: 'VARIABLE_KEY_REQUIRED',
          message: `Component '${component.id}' requires a variableKey.`,
        });
      } else if (!VARIABLE_KEY_PATTERN.test(key)) {
        issues.push({
          componentId: component.id,
          code: 'VARIABLE_KEY_INVALID',
          message: `variableKey '${key}' must match ^[a-zA-Z][a-zA-Z0-9_]{0,63}$.`,
        });
      } else if (seenVariableKeys.has(key)) {
        issues.push({
          componentId: component.id,
          code: 'VARIABLE_KEY_DUPLICATE',
          message: `Duplicate variableKey '${key}'.`,
        });
      } else {
        seenVariableKeys.add(key);
      }
    }

    if (OPTION_TYPES.has(componentType)) {
      if (!component.options || component.options.length === 0) {
        issues.push({
          componentId: component.id,
          code: 'OPTIONS_REQUIRED',
          message: `Component '${component.id}' requires at least one option.`,
        });
      }
    }

    if (component.rules) {
      const ruleEntries: readonly [string, string | undefined][] = [
        ['visibleIf', component.rules.visibleIf],
        ['requiredIf', component.rules.requiredIf],
        ['readOnlyIf', component.rules.readOnlyIf],
      ];
      for (const [ruleName, expr] of ruleEntries) {
        if (expr && expr.trim().length > 0) {
          const syntaxErr = validateExpressionSyntax(expr);
          if (syntaxErr) {
            issues.push({
              componentId: component.id,
              code: 'INVALID_EXPRESSION',
              message: `${ruleName}: ${syntaxErr}`,
            });
          }
        }
      }
    }

    if (component.validators) {
      for (const validator of component.validators) {
        if (validator.type === 'expression' && validator.expression) {
          const syntaxErr = validateExpressionSyntax(validator.expression);
          if (syntaxErr) {
            issues.push({
              componentId: component.id,
              code: 'INVALID_EXPRESSION',
              message: `expression: ${syntaxErr}`,
            });
          }
        }
      }
    }

    if (component.components && component.components.length > 0) {
      for (const child of component.components) {
        visitComponent(child);
      }
    }
  };

  for (const step of schema.steps) {
    for (const component of step.components) {
      visitComponent(component);
    }
  }

  return issues;
}
