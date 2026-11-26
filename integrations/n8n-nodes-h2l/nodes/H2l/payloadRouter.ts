import type { IDataObject, INodeExecutionData } from 'n8n-workflow';

export type H2lWebhookStatus = 'completed' | 'expired' | 'cancelled';

export interface H2lWebhookPayload {
  status: H2lWebhookStatus;
  data: IDataObject;
  _h2l: IDataObject;
}

export const OUTPUT_INDEX: Readonly<Record<H2lWebhookStatus, 0 | 1 | 2>> = {
  completed: 0,
  expired: 1,
  cancelled: 2,
};

function isPlainObject(value: unknown): value is IDataObject {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

export function parseWebhookPayload(body: unknown): H2lWebhookPayload {
  if (!isPlainObject(body)) {
    throw new Error('Webhook payload must be a JSON object');
  }

  const rawStatus = body.status;
  if (typeof rawStatus !== 'string') {
    throw new Error('Webhook payload is missing a valid string "status" field');
  }

  const normalizedStatus = rawStatus.trim().toLowerCase();
  if (
    normalizedStatus !== 'completed' &&
    normalizedStatus !== 'expired' &&
    normalizedStatus !== 'cancelled'
  ) {
    throw new Error(
      `Unsupported h2l webhook status "${rawStatus}". Expected "completed", "expired", or "cancelled"`,
    );
  }

  const data = isPlainObject(body.data) ? body.data : {};
  const metadata = isPlainObject(body._h2l) ? body._h2l : {};

  return {
    status: normalizedStatus,
    data,
    _h2l: metadata,
  };
}

export function routeWebhookPayload(
  payload: H2lWebhookPayload,
): [INodeExecutionData[], INodeExecutionData[], INodeExecutionData[]] {
  const outputs: [INodeExecutionData[], INodeExecutionData[], INodeExecutionData[]] = [[], [], []];
  const targetIndex = OUTPUT_INDEX[payload.status];

  outputs[targetIndex].push({
    json: {
      status: payload.status,
      data: payload.data,
      _h2l: payload._h2l,
    },
  });

  return outputs;
}
