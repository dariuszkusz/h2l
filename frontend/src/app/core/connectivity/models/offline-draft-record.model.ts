export interface OfflineDraftRecord {
  readonly taskId: string;
  readonly payload: Readonly<Record<string, unknown>>;
  readonly updatedAt: string;
}
