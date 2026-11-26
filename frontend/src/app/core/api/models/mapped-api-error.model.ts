export interface MappedApiError {
  readonly status: number;
  readonly errorCode: string | null;
  readonly messageKey: string;
  readonly messageParams?: Readonly<Record<string, string | number>>;
  readonly detail: string | null;
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
  readonly isOffline: boolean;
}
