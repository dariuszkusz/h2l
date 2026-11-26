export type AstNode =
  | { readonly type: 'Literal'; readonly value: string | number | boolean | null }
  | { readonly type: 'Identifier'; readonly name: string }
  | { readonly type: 'Member'; readonly object: AstNode; readonly property: string }
  | { readonly type: 'Unary'; readonly op: '!' | '-'; readonly argument: AstNode }
  | { readonly type: 'Logical'; readonly op: '&&' | '||'; readonly left: AstNode; readonly right: AstNode }
  | { readonly type: 'Ternary'; readonly condition: AstNode; readonly consequent: AstNode; readonly alternate: AstNode }
  | { readonly type: 'Call'; readonly callee: string; readonly args: readonly AstNode[] }
  | { readonly type: 'Binary'; readonly op: string; readonly left: AstNode; readonly right: AstNode };
