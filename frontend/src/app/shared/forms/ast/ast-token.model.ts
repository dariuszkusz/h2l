export type AstToken =
  | { readonly type: 'OP'; readonly value: string }
  | { readonly type: 'PUNCT'; readonly value: string }
  | { readonly type: 'STRING'; readonly value: string }
  | { readonly type: 'NUMBER'; readonly value: number }
  | { readonly type: 'BOOLEAN'; readonly value: boolean }
  | { readonly type: 'NULL'; readonly value: null }
  | { readonly type: 'IDENT'; readonly value: string }
  | { readonly type: 'EOF'; readonly value: null };
