import { describe, expect, it } from 'vitest';

import { parseWebhookPayload, routeWebhookPayload } from '../nodes/H2l/payloadRouter';

describe('payloadRouter', () => {
  it('routes "completed" status to output index 0 (Completed)', () => {
    const payload = parseWebhookPayload({
      status: 'completed',
      data: { softwareName: 'Rider', estimatedCost: 1490 },
      _h2l: {
        taskId: 'task-1',
        submissionId: 'sub-1',
        formVersion: 2,
        sensitiveKeys: ['vendorIban'],
      },
    });

    const [completed, expired, cancelled] = routeWebhookPayload(payload);

    expect(completed).toHaveLength(1);
    expect(completed[0].json).toEqual({
      status: 'completed',
      data: { softwareName: 'Rider', estimatedCost: 1490 },
      _h2l: {
        taskId: 'task-1',
        submissionId: 'sub-1',
        formVersion: 2,
        sensitiveKeys: ['vendorIban'],
      },
    });
    expect(expired).toHaveLength(0);
    expect(cancelled).toHaveLength(0);
  });

  it('routes "expired" status to output index 1 (Expired)', () => {
    const payload = parseWebhookPayload({
      status: 'expired',
      data: {},
      _h2l: { taskId: 'task-2', reason: 'TASK_TIMEOUT_EXCEEDED' },
    });

    const [completed, expired, cancelled] = routeWebhookPayload(payload);

    expect(completed).toHaveLength(0);
    expect(expired).toHaveLength(1);
    expect(expired[0].json.status).toBe('expired');
    expect(cancelled).toHaveLength(0);
  });

  it('routes "cancelled" status to output index 2 (Cancelled)', () => {
    const payload = parseWebhookPayload({
      status: 'cancelled',
      data: {},
      _h2l: { taskId: 'task-3', reason: 'ADMIN_CANCELLED' },
    });

    const [completed, expired, cancelled] = routeWebhookPayload(payload);

    expect(completed).toHaveLength(0);
    expect(expired).toHaveLength(0);
    expect(cancelled).toHaveLength(1);
    expect(cancelled[0].json.status).toBe('cancelled');
  });

  it('throws on unsupported status values', () => {
    expect(() => parseWebhookPayload({ status: 'unknown', data: {}, _h2l: {} })).toThrow(
      /Unsupported h2l webhook status/,
    );
  });
});
