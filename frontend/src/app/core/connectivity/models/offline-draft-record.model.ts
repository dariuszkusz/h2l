export interface OfflineDraftRecord {
  readonly taskId: string;
  readonly payload: Readonly<Record<string, unknown>>;
  readonly updatedAt: string;
  readonly queuedForSubmission?: boolean;
  readonly idempotencyKey?: string | null;
  readonly attachmentIds?: readonly string[];
}
