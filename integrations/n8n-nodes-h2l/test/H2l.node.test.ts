import type { IExecuteFunctions, IWebhookFunctions } from 'n8n-workflow';
import { describe, expect, it, vi } from 'vitest';

vi.mock('n8n-workflow', () => ({
  NodeOperationError: class NodeOperationError extends Error {
    constructor(_node: unknown, message: string) {
      super(message);
      this.name = 'NodeOperationError';
    }
  },
}));

import { H2l, WAIT_TILL_INDEFINITE } from '../nodes/H2l/H2l.node';
import { computeHmacSignature } from '../nodes/H2l/hmacVerifier';

describe('H2l node', () => {
  const node = new H2l();

  it('sends Wait for Human Form request with resumeUrl and puts execution to wait', async () => {
    const httpRequestWithAuthentication = vi.fn().mockResolvedValue({
      taskId: '0193f1a0-7c4b-7000-8000-000000000010',
      status: 'Pending',
      isDuplicate: false,
    });
    const putExecutionToWait = vi.fn().mockResolvedValue(undefined);

    const params: Record<string, unknown> = {
      operation: 'waitForHumanForm',
      connectorId: '',
      formId: '0193f1a0-7c4b-7000-8000-000000000099',
      formVersion: 2,
      assignee: 'approver@example.com',
      candidateGroups: ['finance', 'procurement'],
      timeoutIso8601: 'P3D',
      sensitiveDataMode: 'ClaimCheck',
      prefilledData: '{"requestor":"Alice"}',
      externalTaskId: '',
    };

    const mockContext = {
      getInputData: () => [{ json: { orderId: 42 } }],
      getNodeParameter: (name: string, _index: number, fallback?: unknown) =>
        name in params ? params[name] : fallback,
      getCredentials: vi.fn().mockResolvedValue({
        baseUrl: 'https://h2l.example.com/',
        apiKey: 'secret-api-key',
        webhookSecret: 'hmac-secret-123',
        connectorId: '0193f1a0-7c4b-7000-8000-000000000001',
      }),
      getExecutionId: () => 'exec-123',
      getExecutionResumeUrl: () => 'https://n8n.example.com/webhook-waiting/exec-123',
      getInstanceBaseUrl: () => 'https://n8n.example.com',
      getNode: () => ({ name: 'Approval Form' }),
      putExecutionToWait,
      helpers: {
        httpRequestWithAuthentication,
      },
    } as unknown as IExecuteFunctions;

    const result = await node.execute.call(mockContext);

    expect(httpRequestWithAuthentication).toHaveBeenCalledTimes(1);
    const [credName, requestOptions] = httpRequestWithAuthentication.mock.calls[0];
    expect(credName).toBe('h2lApi');
    expect(requestOptions.method).toBe('POST');
    expect(requestOptions.url).toBe(
      'https://h2l.example.com/api/v1/webhooks/connectors/0193f1a0-7c4b-7000-8000-000000000001/tasks',
    );
    expect(requestOptions.headers['X-H2L-Timestamp']).toMatch(/^\d+$/);
    expect(requestOptions.headers['X-H2L-Signature']).toMatch(/^sha256=[a-f0-9]{64}$/);

    const parsedBody = JSON.parse(requestOptions.body as string);
    expect(parsedBody).toEqual({
      externalTaskId: 'n8n:exec-123:Approval Form',
      formId: '0193f1a0-7c4b-7000-8000-000000000099',
      formVersion: 2,
      assignee: 'approver@example.com',
      candidateGroups: ['finance', 'procurement'],
      prefilledData: { requestor: 'Alice' },
      resumeUrl: 'https://n8n.example.com/webhook-waiting/exec-123',
      timeoutIso8601: 'P3D',
      sensitiveDataMode: 'ClaimCheck',
    });

    expect(putExecutionToWait).toHaveBeenCalledWith(WAIT_TILL_INDEFINITE);
    expect(result).toHaveLength(3);
  });

  it('calls GET /api/v1/submissions/{submissionId}/sensitive-data for Get Sensitive Data', async () => {
    const httpRequestWithAuthentication = vi.fn().mockResolvedValue({
      submissionId: '0193f1b2-9a11-7000-8000-000000000025',
      values: { vendorIban: 'DE89370400440532013000' },
    });

    const mockContext = {
      getInputData: () => [{ json: { _h2l: { submissionId: '0193f1b2-9a11-7000-8000-000000000025' } } }],
      getNodeParameter: (name: string) =>
        name === 'operation' ? 'getSensitiveData' : '0193f1b2-9a11-7000-8000-000000000025',
      getCredentials: vi.fn().mockResolvedValue({
        baseUrl: 'https://h2l.example.com',
        apiKey: 'secret-api-key',
      }),
      getNode: () => ({ name: 'Fetch Sensitive Data' }),
      helpers: {
        httpRequestWithAuthentication,
      },
    } as unknown as IExecuteFunctions;

    const result = await node.execute.call(mockContext);

    expect(httpRequestWithAuthentication).toHaveBeenCalledWith('h2lApi', {
      method: 'GET',
      url: 'https://h2l.example.com/api/v1/submissions/0193f1b2-9a11-7000-8000-000000000025/sensitive-data',
      json: true,
    });
    expect(result[0]).toHaveLength(1);
    expect(result[0][0].json).toEqual({
      submissionId: '0193f1b2-9a11-7000-8000-000000000025',
      values: { vendorIban: 'DE89370400440532013000' },
    });
  });

  it('rejects invalid HMAC signature in webhook() with HTTP 401', async () => {
    const jsonMock = vi.fn();
    const statusMock = vi.fn().mockReturnValue({ json: jsonMock });

    const body = { status: 'completed', data: {}, _h2l: { taskId: 'task-1' } };
    const mockWebhookContext = {
      getRequestObject: () => ({
        headers: {
          'x-h2l-timestamp': String(Math.floor(Date.now() / 1000)),
          'x-h2l-signature': 'sha256=deadbeef',
        },
        rawBody: Buffer.from(JSON.stringify(body), 'utf8'),
      }),
      getResponseObject: () => ({ status: statusMock }),
      getBodyData: () => body,
      getCredentials: vi.fn().mockResolvedValue({
        baseUrl: 'https://h2l.example.com',
        apiKey: 'secret-api-key',
        webhookSecret: 'real-secret',
      }),
    } as unknown as IWebhookFunctions;

    const response = await node.webhook.call(mockWebhookContext);

    expect(statusMock).toHaveBeenCalledWith(401);
    expect(jsonMock).toHaveBeenCalledWith(expect.objectContaining({ error: 'Unauthorized' }));
    expect(response).toEqual({ noWebhookResponse: true });
  });

  it('verifies HMAC signature and routes completed callback to output 0 in webhook()', async () => {
    const secret = 'real-secret';
    const body = {
      status: 'completed',
      data: { decision: 'approve' },
      _h2l: { taskId: 'task-1', submissionId: 'sub-1', sensitiveKeys: [] },
    };
    const rawBody = JSON.stringify(body);
    const timestamp = String(Math.floor(Date.now() / 1000));
    const signature = computeHmacSignature(secret, timestamp, rawBody);

    const mockWebhookContext = {
      getRequestObject: () => ({
        headers: {
          'x-h2l-timestamp': timestamp,
          'x-h2l-signature': signature,
        },
        rawBody: Buffer.from(rawBody, 'utf8'),
      }),
      getResponseObject: () => ({ status: vi.fn() }),
      getBodyData: () => body,
      getCredentials: vi.fn().mockResolvedValue({
        baseUrl: 'https://h2l.example.com',
        apiKey: 'secret-api-key',
        webhookSecret: secret,
      }),
    } as unknown as IWebhookFunctions;

    const response = await node.webhook.call(mockWebhookContext);

    expect(response.workflowData).toBeDefined();
    expect(response.workflowData?.[0]).toHaveLength(1);
    expect(response.workflowData?.[0][0].json).toEqual(body);
    expect(response.workflowData?.[1]).toHaveLength(0);
    expect(response.workflowData?.[2]).toHaveLength(0);
  });
});
