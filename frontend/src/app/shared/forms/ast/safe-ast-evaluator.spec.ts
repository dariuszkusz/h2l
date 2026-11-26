import { describe, expect, it } from 'vitest';
import {
  evaluateBooleanExpression,
  evaluateExpression,
  parseExpression,
  validateExpressionSyntax,
} from './safe-ast-evaluator';
import { SafeExpressionError } from './safe-expression-error';

describe('safe-ast-evaluator', () => {
  it('evaluates comparisons, logical, ternary, and member length expressions', () => {
    expect(
      evaluateExpression(
        'estimatedCost <= 1000 || (businessJustification && businessJustification.length >= 20)',
        { estimatedCost: 1500, businessJustification: 'Urgent license renewal for team' },
      ),
    ).toBe(true);

    expect(
      evaluateExpression(
        'estimatedCost <= 1000 || (businessJustification && businessJustification.length >= 20)',
        { estimatedCost: 1500, businessJustification: 'short' },
      ),
    ).toBe(false);

    expect(evaluateExpression('amount > 50 ? "high" : "low"', { amount: 75 })).toBe('high');
  });

  it('supports whitelisted pure helper functions', () => {
    expect(evaluateExpression('len(name) >= 4 && !isEmpty(name)', { name: 'Alice' })).toBe(true);
    expect(evaluateExpression('includes(roles, "admin")', { roles: ['user', 'admin'] })).toBe(true);
    expect(evaluateExpression('upper("ok") === "OK" && lower("NO") === "no"', {})).toBe(true);
  });

  it('blocks prototype pollution and non-whitelisted function calls', () => {
    expect(() => parseExpression('user.__proto__.polluted')).toThrow(SafeExpressionError);
    expect(() => parseExpression('constructor.prototype')).toThrow(SafeExpressionError);
    expect(() => parseExpression('alert(1)')).toThrow(SafeExpressionError);
  });

  it('enforces max expression length and AST depth limits', () => {
    const tooLong = 'a + '.repeat(400) + 'a';
    expect(() => parseExpression(tooLong)).toThrow(SafeExpressionError);

    const tooDeep = '('.repeat(40) + '1' + ')'.repeat(40);
    expect(() => parseExpression(tooDeep)).toThrow(SafeExpressionError);
  });

  it('returns default fallback in evaluateBooleanExpression and reports syntax errors', () => {
    expect(evaluateBooleanExpression('', {}, true)).toBe(true);
    expect(evaluateBooleanExpression('invalid ???', {}, false)).toBe(false);
    expect(validateExpressionSyntax('amount > 10')).toBeNull();
    expect(validateExpressionSyntax('amount >')).not.toBeNull();
  });
});
