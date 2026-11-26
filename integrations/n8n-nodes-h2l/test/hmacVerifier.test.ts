import { describe, expect, it } from 'vitest';

import {
  computeHmacSignature,
  MAX_TIMESTAMP_SKEW_SECONDS,
  verifyWebhookRequest,
} from '../nodes/H2l/hmacVerifier';

describe('hmacVerifier', () => {
  const secret = 'test-hmac-secret-0123456789';
  const apiKey = 'test-api-key-abcdef';
  const now = 1_700_000_000;
  const rawBody = JSON.stringify({
    status: 'completed',
    data: { approved: true },
    _h2l: { taskId: 'task-1', submissionId: 'sub-1', sensitiveKeys: ['iban'] },
  });

  it('accepts a valid HMAC-SHA256 signature within the ±300s window', () => {
    const timestamp = String(now - 60);
    const signature = computeHmacSignature(secret, timestamp, rawBody);

    const result = verifyWebhookRequest(
      {
        'x-h2l-timestamp': timestamp,
        'x-h2l-signature': signature,
      },
      rawBody,
      { webhookSecret: secret, apiKey },
      now,
    );

    expect(result).toEqual({ valid: true, method: 'hmac-sha256' });
  });

  it('rejects a tampered body with HMAC-SHA256', () => {
    const timestamp = String(now);
    const signature = computeHmacSignature(secret, timestamp, rawBody);
    const tamperedBody = rawBody.replace('true', 'false');

    const result = verifyWebhookRequest(
      {
        'X-H2L-Timestamp': timestamp,
        'X-H2L-Signature': signature,
      },
      tamperedBody,
      { webhookSecret: secret, apiKey },
      now,
    );

    expect(result.valid).toBe(false);
  });

  it('rejects an expired timestamp older than 5 minutes (>300s)', () => {
    const expiredTimestamp = String(now - MAX_TIMESTAMP_SKEW_SECONDS - 1);
    const signature = computeHmacSignature(secret, expiredTimestamp, rawBody);

    const result = verifyWebhookRequest(
      {
        'x-h2l-timestamp': expiredTimestamp,
        'x-h2l-signature': signature,
      },
      rawBody,
      { webhookSecret: secret, apiKey },
      now,
    );

    expect(result.valid).toBe(false);
    if (!result.valid) {
      expect(result.reason).toContain('±300');
    }
  });

  it('rejects a future timestamp beyond 5 minutes (>300s)', () => {
    const futureTimestamp = String(now + MAX_TIMESTAMP_SKEW_SECONDS + 1);
    const signature = computeHmacSignature(secret, futureTimestamp, rawBody);

    const result = verifyWebhookRequest(
      {
        'x-h2l-timestamp': futureTimestamp,
        'x-h2l-signature': signature,
      },
      rawBody,
      { webhookSecret: secret, apiKey },
      now,
    );

    expect(result.valid).toBe(false);
    if (!result.valid) {
      expect(result.reason).toContain('±300');
    }
  });

  it('rejects missing HMAC headers when webhookSecret is configured', () => {
    const result = verifyWebhookRequest(
      { authorization: `Bearer ${apiKey}` },
      rawBody,
      { webhookSecret: secret, apiKey },
      now,
    );

    expect(result.valid).toBe(false);
  });

  it('accepts Bearer token fallback when webhookSecret is not configured', () => {
    const result = verifyWebhookRequest(
      { Authorization: `Bearer ${apiKey}` },
      rawBody,
      { apiKey },
      now,
    );

    expect(result).toEqual({ valid: true, method: 'bearer' });
  });

  it('accepts X-API-Key fallback when webhookSecret is not configured', () => {
    const result = verifyWebhookRequest(
      { 'X-API-Key': apiKey },
      rawBody,
      { apiKey },
      now,
    );

    expect(result).toEqual({ valid: true, method: 'api-key' });
  });

  it('rejects invalid fallback credentials', () => {
    const result = verifyWebhookRequest(
      { Authorization: 'Bearer wrong-token' },
      rawBody,
      { apiKey },
      now,
    );

    expect(result.valid).toBe(false);
  });
});
