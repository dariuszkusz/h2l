export interface SchemaValidationIssue {
  readonly componentId: string | null;
  readonly code:
    | 'EMPTY_STEPS'
    | 'VARIABLE_KEY_REQUIRED'
    | 'VARIABLE_KEY_INVALID'
    | 'VARIABLE_KEY_DUPLICATE'
    | 'INVALID_EXPRESSION'
    | 'OPTIONS_REQUIRED';
  readonly message: string;
}
