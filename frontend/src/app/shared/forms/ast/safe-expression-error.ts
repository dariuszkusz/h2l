export class SafeExpressionError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'SafeExpressionError';
  }
}
