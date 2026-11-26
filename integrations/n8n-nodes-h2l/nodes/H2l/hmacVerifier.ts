import { createHmac, timingSafeEqual } from 'node:crypto';

export const MAX_TIMESTAMP_SKEW_SECONDS = 300;
export const SIGNATURE_PREFIX = 'sha256=';

export interface WebhookAuthConfig {
  webhookSecret?: string;
  apiKey?: string;
}

export type WebhookVerificationResult =
  | { valid: true; method: 'hmac-sha256' | 'bearer' | 'api-key' }
  | { valid: false; reason: string };

export type IncomingHeaders = Record<string, string | string[] | undefined>;

function getNormalizedHeader(headers: IncomingHeaders, targetName: string): string | undefined {
  const lowerTarget = targetName.toLowerCase();
  for (const [key, value] of Object.entries(headers)) {
    if (key.toLowerCase() !== lowerTarget) {
      continue;
    }
    if (Array.isArray(value)) {
      return value[0]?.trim();
    }
    return value?.trim();
  }
  return undefined;
}

export function timingSafeStringEqual(expected: string, actual: string): boolean {
  const expectedBuffer = Buffer.from(expected, 'utf8');
  const actualBuffer = Buffer.from(actual, 'utf8');

  if (expectedBuffer.length !== actualBuffer.length) {
    // Perform a dummy constant-time comparison to avoid short-circuit timing variance
    timingSafeEqual(expectedBuffer, expectedBuffer);
    return false;
  }

  return timingSafeEqual(expectedBuffer, actualBuffer);
}

export function computeHmacSignature(
  secret: string,
  timestampSeconds: string,
  rawBody: string,
): string {
  const digest = createHmac('sha256', secret)
    .update(`${timestampSeconds}.${rawBody}`, 'utf8')
    .digest('hex');

  return `${SIGNATURE_PREFIX}${digest}`;
}

export function verifyWebhookRequest(
  headers: IncomingHeaders,
  rawBody: string,
  config: WebhookAuthConfig,
  nowSeconds: number = Math.floor(Date.now() / 1000),
): WebhookVerificationResult {
  const secret = config.webhookSecret?.trim();

  if (secret) {
    const timestampHeader = getNormalizedHeader(headers, 'x-h2l-timestamp');
    const signatureHeader = getNormalizedHeader(headers, 'x-h2l-signature');

    if (!timestampHeader || !signatureHeader) {
      return {
        valid: false,
        reason: 'Missing X-H2L-Timestamp or X-H2L-Signature header',
      };
    }

    if (!/^\d+$/.test(timestampHeader)) {
      return {
        valid: false,
        reason: 'Invalid X-H2L-Timestamp format',
      };
    }

    const parsedTimestamp = Number.parseInt(timestampHeader, 10);
    if (!Number.isSafeInteger(parsedTimestamp)) {
      return {
        valid: false,
        reason: 'Invalid X-H2L-Timestamp value',
      };
    }

    if (Math.abs(nowSeconds - parsedTimestamp) > MAX_TIMESTAMP_SKEW_SECONDS) {
      return {
        valid: false,
        reason: 'Webhook timestamp outside allowed ±300 seconds replay window',
      };
    }

    if (!signatureHeader.toLowerCase().startsWith(SIGNATURE_PREFIX)) {
      return {
        valid: false,
        reason: 'Invalid X-H2L-Signature prefix',
      };
    }

    const expectedSignature = computeHmacSignature(secret, timestampHeader, rawBody);
    const normalizedProvided = `${SIGNATURE_PREFIX}${signatureHeader.slice(SIGNATURE_PREFIX.length).toLowerCase()}`;

    if (!timingSafeStringEqual(expectedSignature, normalizedProvided)) {
      return {
        valid: false,
        reason: 'Invalid HMAC-SHA256 signature',
      };
    }

    return { valid: true, method: 'hmac-sha256' };
  }

  const apiKey = config.apiKey?.trim();
  if (!apiKey) {
    return {
      valid: false,
      reason: 'No webhook secret or API key configured in h2l credentials',
    };
  }

  const authorizationHeader = getNormalizedHeader(headers, 'authorization');
  if (authorizationHeader?.toLowerCase().startsWith('bearer ')) {
    const bearerToken = authorizationHeader.slice('bearer '.length).trim();
    if (bearerToken.length > 0 && timingSafeStringEqual(apiKey, bearerToken)) {
      return { valid: true, method: 'bearer' };
    }
  }

  const apiKeyHeader = getNormalizedHeader(headers, 'x-api-key');
  if (apiKeyHeader && timingSafeStringEqual(apiKey, apiKeyHeader)) {
    return { valid: true, method: 'api-key' };
  }

  return {
    valid: false,
    reason: 'Invalid or missing Bearer token / X-API-Key header',
  };
}
