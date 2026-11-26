import { AstNode } from './ast-node.model';
import { AstToken } from './ast-token.model';
import { SafeExpressionError } from './safe-expression-error';

const MAX_EXPRESSION_LENGTH = 1024;
const MAX_AST_DEPTH = 32;

const FORBIDDEN_PROPERTIES: ReadonlySet<string> = new Set([
  '__proto__',
  'prototype',
  'constructor',
  '__defineGetter__',
  '__defineSetter__',
  '__lookupGetter__',
  '__lookupSetter__',
]);

type WhitelistedFn = (...args: readonly unknown[]) => unknown;

const WHITELISTED_FUNCTIONS: Readonly<Record<string, WhitelistedFn>> = Object.freeze({
  len: (value: unknown): number => {
    if (typeof value === 'string' || Array.isArray(value)) {
      return value.length;
    }
    return 0;
  },
  isEmpty: (value: unknown): boolean => {
    if (value === null || value === undefined) {
      return true;
    }
    if (typeof value === 'string') {
      return value.trim().length === 0;
    }
    if (Array.isArray(value)) {
      return value.length === 0;
    }
    return false;
  },
  includes: (collection: unknown, item: unknown): boolean => {
    if (typeof collection === 'string') {
      return collection.includes(String(item));
    }
    if (Array.isArray(collection)) {
      return collection.includes(item);
    }
    return false;
  },
  lower: (value: unknown): string => (typeof value === 'string' ? value.toLowerCase() : ''),
  upper: (value: unknown): string => (typeof value === 'string' ? value.toUpperCase() : ''),
});

export function tokenizeExpression(source: string): readonly AstToken[] {
  if (typeof source !== 'string') {
    throw new SafeExpressionError('Expression must be a string.');
  }
  if (source.length > MAX_EXPRESSION_LENGTH) {
    throw new SafeExpressionError(`Expression exceeds max length (${MAX_EXPRESSION_LENGTH}).`);
  }

  const tokens: AstToken[] = [];
  let i = 0;

  while (i < source.length) {
    const ch = source[i];

    if (/\s/.test(ch)) {
      i++;
      continue;
    }

    const three = source.slice(i, i + 3);
    if (three === '===' || three === '!==') {
      tokens.push({ type: 'OP', value: three });
      i += 3;
      continue;
    }

    const two = source.slice(i, i + 2);
    if (two === '<=' || two === '>=' || two === '==' || two === '!=' || two === '&&' || two === '||') {
      tokens.push({ type: 'OP', value: two });
      i += 2;
      continue;
    }

    if ('().,?:+-*/%<>!'.includes(ch)) {
      tokens.push({ type: 'PUNCT', value: ch });
      i++;
      continue;
    }

    if (ch === "'" || ch === '"') {
      const quote = ch;
      let str = '';
      i++;
      let closed = false;
      while (i < source.length) {
        if (source[i] === '\\' && i + 1 < source.length) {
          str += source[i + 1];
          i += 2;
          continue;
        }
        if (source[i] === quote) {
          closed = true;
          i++;
          break;
        }
        str += source[i];
        i++;
      }
      if (!closed) {
        throw new SafeExpressionError('Unterminated string literal.');
      }
      tokens.push({ type: 'STRING', value: str });
      continue;
    }

    if (/[0-9]/.test(ch)) {
      let numStr = '';
      let hasDot = false;
      while (i < source.length && (/[0-9]/.test(source[i]) || (source[i] === '.' && !hasDot))) {
        if (source[i] === '.') {
          hasDot = true;
        }
        numStr += source[i];
        i++;
      }
      tokens.push({ type: 'NUMBER', value: Number(numStr) });
      continue;
    }

    if (/[a-zA-Z_$]/.test(ch)) {
      let ident = '';
      while (i < source.length && /[a-zA-Z0-9_$]/.test(source[i])) {
        ident += source[i];
        i++;
      }
      if (ident === 'true' || ident === 'false') {
        tokens.push({ type: 'BOOLEAN', value: ident === 'true' });
      } else if (ident === 'null') {
        tokens.push({ type: 'NULL', value: null });
      } else {
        tokens.push({ type: 'IDENT', value: ident });
      }
      continue;
    }

    throw new SafeExpressionError(`Unexpected character '${ch}' at position ${i}.`);
  }

  tokens.push({ type: 'EOF', value: null });
  return tokens;
}

export function parseExpression(source: string): AstNode {
  const tokens = tokenizeExpression(source);
  let pos = 0;

  const peek = (): AstToken => tokens[pos];
  const consume = (): AstToken => tokens[pos++];

  function expect(type: AstToken['type'], value?: string): AstToken {
    const token = peek();
    if (token.type !== type || (value !== undefined && token.value !== value)) {
      throw new SafeExpressionError(`Expected ${value ?? type}, got '${String(token.value ?? token.type)}'.`);
    }
    return consume();
  }

  function parseTernary(depth: number): AstNode {
    if (depth > MAX_AST_DEPTH) {
      throw new SafeExpressionError(`Expression exceeds max AST depth (${MAX_AST_DEPTH}).`);
    }
    const condition = parseOr(depth + 1);
    const next = peek();
    if (next.type === 'PUNCT' && next.value === '?') {
      consume();
      const consequent = parseTernary(depth + 1);
      expect('PUNCT', ':');
      const alternate = parseTernary(depth + 1);
      return { type: 'Ternary', condition, consequent, alternate };
    }
    return condition;
  }

  function parseOr(depth: number): AstNode {
    let left = parseAnd(depth + 1);
    while (true) {
      const next = peek();
      if (next.type === 'OP' && next.value === '||') {
        consume();
        const right = parseAnd(depth + 1);
        left = { type: 'Logical', op: '||', left, right };
      } else {
        break;
      }
    }
    return left;
  }

  function parseAnd(depth: number): AstNode {
    let left = parseEquality(depth + 1);
    while (true) {
      const next = peek();
      if (next.type === 'OP' && next.value === '&&') {
        consume();
        const right = parseEquality(depth + 1);
        left = { type: 'Logical', op: '&&', left, right };
      } else {
        break;
      }
    }
    return left;
  }

  function parseEquality(depth: number): AstNode {
    let left = parseComparison(depth + 1);
    while (true) {
      const next = peek();
      if (
        next.type === 'OP' &&
        (next.value === '==' || next.value === '!=' || next.value === '===' || next.value === '!==')
      ) {
        const op = consume().value as string;
        const right = parseComparison(depth + 1);
        left = { type: 'Binary', op, left, right };
      } else {
        break;
      }
    }
    return left;
  }

  function parseComparison(depth: number): AstNode {
    let left = parseAdditive(depth + 1);
    while (true) {
      const next = peek();
      if (
        (next.type === 'PUNCT' && (next.value === '<' || next.value === '>')) ||
        (next.type === 'OP' && (next.value === '<=' || next.value === '>='))
      ) {
        const op = consume().value as string;
        const right = parseAdditive(depth + 1);
        left = { type: 'Binary', op, left, right };
      } else {
        break;
      }
    }
    return left;
  }

  function parseAdditive(depth: number): AstNode {
    let left = parseMultiplicative(depth + 1);
    while (true) {
      const next = peek();
      if (next.type === 'PUNCT' && (next.value === '+' || next.value === '-')) {
        const op = consume().value as string;
        const right = parseMultiplicative(depth + 1);
        left = { type: 'Binary', op, left, right };
      } else {
        break;
      }
    }
    return left;
  }

  function parseMultiplicative(depth: number): AstNode {
    let left = parseUnary(depth + 1);
    while (true) {
      const next = peek();
      if (next.type === 'PUNCT' && (next.value === '*' || next.value === '/' || next.value === '%')) {
        const op = consume().value as string;
        const right = parseUnary(depth + 1);
        left = { type: 'Binary', op, left, right };
      } else {
        break;
      }
    }
    return left;
  }

  function parseUnary(depth: number): AstNode {
    const next = peek();
    if (next.type === 'PUNCT' && (next.value === '!' || next.value === '-')) {
      const op = consume().value as '!' | '-';
      const argument = parseUnary(depth + 1);
      return { type: 'Unary', op, argument };
    }
    return parseMember(depth + 1);
  }

  function parseMember(depth: number): AstNode {
    let node = parsePrimary(depth + 1);
    while (true) {
      const next = peek();
      if (next.type === 'PUNCT' && next.value === '.') {
        consume();
        const propToken = expect('IDENT');
        const property = propToken.value as string;
        if (FORBIDDEN_PROPERTIES.has(property)) {
          throw new SafeExpressionError(`Access to forbidden property '${property}' is blocked.`);
        }
        node = { type: 'Member', object: node, property };
      } else {
        break;
      }
    }
    return node;
  }

  function parsePrimary(depth: number): AstNode {
    if (depth > MAX_AST_DEPTH) {
      throw new SafeExpressionError(`Expression exceeds max AST depth (${MAX_AST_DEPTH}).`);
    }

    const token = peek();

    if (
      token.type === 'NUMBER' ||
      token.type === 'STRING' ||
      token.type === 'BOOLEAN' ||
      token.type === 'NULL'
    ) {
      consume();
      return { type: 'Literal', value: token.value };
    }

    if (token.type === 'IDENT') {
      const name = (consume() as { type: 'IDENT'; value: string }).value;
      if (FORBIDDEN_PROPERTIES.has(name)) {
        throw new SafeExpressionError(`Identifier '${name}' is forbidden.`);
      }

      const afterIdent = peek();
      if (afterIdent.type === 'PUNCT' && afterIdent.value === '(') {
        if (!Object.prototype.hasOwnProperty.call(WHITELISTED_FUNCTIONS, name)) {
          throw new SafeExpressionError(`Function '${name}' is not whitelisted.`);
        }
        consume();
        const args: AstNode[] = [];
        const maybeClose = peek();
        if (!(maybeClose.type === 'PUNCT' && maybeClose.value === ')')) {
          args.push(parseTernary(depth + 1));
          while (true) {
            const sep = peek();
            if (sep.type === 'PUNCT' && sep.value === ',') {
              consume();
              args.push(parseTernary(depth + 1));
            } else {
              break;
            }
          }
        }
        expect('PUNCT', ')');
        return { type: 'Call', callee: name, args };
      }

      return { type: 'Identifier', name };
    }

    if (token.type === 'PUNCT' && token.value === '(') {
      consume();
      const inner = parseTernary(depth + 1);
      expect('PUNCT', ')');
      return inner;
    }

    throw new SafeExpressionError(`Unexpected token '${String(token.value ?? token.type)}'.`);
  }

  const ast = parseTernary(0);
  if (peek().type !== 'EOF') {
    throw new SafeExpressionError(`Unexpected trailing token '${String(peek().value)}'.`);
  }
  return ast;
}

export function evaluateAst(node: AstNode, context: Readonly<Record<string, unknown>> = {}): unknown {
  switch (node.type) {
    case 'Literal':
      return node.value;

    case 'Identifier':
      return Object.prototype.hasOwnProperty.call(context, node.name)
        ? context[node.name]
        : undefined;

    case 'Member': {
      const obj = evaluateAst(node.object, context);
      if (obj === null || obj === undefined) {
        return undefined;
      }
      if (FORBIDDEN_PROPERTIES.has(node.property)) {
        throw new SafeExpressionError(`Access to forbidden property '${node.property}' is blocked.`);
      }
      if (node.property === 'length' && (typeof obj === 'string' || Array.isArray(obj))) {
        return obj.length;
      }
      if (typeof obj === 'object' && Object.prototype.hasOwnProperty.call(obj, node.property)) {
        return (obj as Record<string, unknown>)[node.property];
      }
      return undefined;
    }

    case 'Unary': {
      const val = evaluateAst(node.argument, context);
      if (node.op === '!') {
        return !val;
      }
      return -Number(val);
    }

    case 'Logical': {
      const left = evaluateAst(node.left, context);
      if (node.op === '&&') {
        return left ? evaluateAst(node.right, context) : left;
      }
      return left ? left : evaluateAst(node.right, context);
    }

    case 'Ternary':
      return evaluateAst(node.condition, context)
        ? evaluateAst(node.consequent, context)
        : evaluateAst(node.alternate, context);

    case 'Call': {
      const fn = WHITELISTED_FUNCTIONS[node.callee];
      if (!fn) {
        throw new SafeExpressionError(`Function '${node.callee}' is not whitelisted.`);
      }
      const evaluatedArgs = node.args.map((arg) => evaluateAst(arg, context));
      return fn(...evaluatedArgs);
    }

    case 'Binary': {
      const left = evaluateAst(node.left, context);
      const right = evaluateAst(node.right, context);
      switch (node.op) {
        case '+':
          if (typeof left === 'string' || typeof right === 'string') {
            return `${String(left ?? '')}${String(right ?? '')}`;
          }
          return Number(left) + Number(right);
        case '-':
          return Number(left) - Number(right);
        case '*':
          return Number(left) * Number(right);
        case '/':
          return Number(right) === 0 ? Number.NaN : Number(left) / Number(right);
        case '%':
          return Number(right) === 0 ? Number.NaN : Number(left) % Number(right);
        case '<':
          return (left as number | string) < (right as number | string);
        case '<=':
          return (left as number | string) <= (right as number | string);
        case '>':
          return (left as number | string) > (right as number | string);
        case '>=':
          return (left as number | string) >= (right as number | string);
        case '==':
          return left == right;
        case '!=':
          return left != right;
        case '===':
          return left === right;
        case '!==':
          return left !== right;
        default:
          throw new SafeExpressionError(`Unsupported binary operator '${node.op}'.`);
      }
    }
  }
}

export function evaluateExpression(
  source: string,
  context: Readonly<Record<string, unknown>> = {},
): unknown {
  const ast = parseExpression(source);
  return evaluateAst(ast, context);
}

export function evaluateBooleanExpression(
  source: string | undefined | null,
  context: Readonly<Record<string, unknown>>,
  defaultWhenEmpty: boolean,
): boolean {
  if (!source || source.trim().length === 0) {
    return defaultWhenEmpty;
  }
  try {
    return Boolean(evaluateExpression(source, context));
  } catch {
    return defaultWhenEmpty;
  }
}

export function validateExpressionSyntax(source: string): string | null {
  if (!source || source.trim().length === 0) {
    return null;
  }
  try {
    parseExpression(source);
    return null;
  } catch (err: unknown) {
    return err instanceof Error ? err.message : 'Invalid expression.';
  }
}
