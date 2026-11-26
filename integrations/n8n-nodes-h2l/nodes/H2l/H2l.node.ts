import type {
  IDataObject,
  IExecuteFunctions,
  ILoadOptionsFunctions,
  INodeExecutionData,
  INodePropertyOptions,
  INodeType,
  INodeTypeDescription,
  IWebhookFunctions,
  IWebhookResponseData,
  NodeConnectionType,
} from 'n8n-workflow';
import { NodeOperationError } from 'n8n-workflow';

import {
  buildCreateTaskPayload,
  normalizeBaseUrl,
  type SensitiveDataMode,
} from './h2lRequestBuilder';
import { computeHmacSignature, verifyWebhookRequest } from './hmacVerifier';
import { parseWebhookPayload, routeWebhookPayload } from './payloadRouter';

export const WAIT_TILL_INDEFINITE = new Date('3000-01-01T00:00:00.000Z');

interface H2lCredentials {
  baseUrl: string;
  apiKey: string;
  webhookSecret?: string;
  connectorId?: string;
}

interface H2lProject {
  id: string;
  name: string;
}

interface H2lProcess {
  id: string;
  name: string;
}

interface H2lFormDefinition {
  id: string;
  name: string;
  stepKey: string;
}

interface H2lGroup {
  id: string;
  name: string;
  code: string;
}

interface RawBodyRequest {
  headers: Record<string, string | string[] | undefined>;
  rawBody?: Buffer | string;
}

interface JsonResponse {
  status(code: number): { json(payload: IDataObject): void };
}

interface ResumeUrlCapableContext {
  getExecutionResumeUrl?: () => string;
  getNodeWebhookUrl?: (name: string) => string | undefined;
}

function resolveResumeUrl(context: IExecuteFunctions): string {
  const extended = context as unknown as ResumeUrlCapableContext;
  const executionResumeUrl = extended.getExecutionResumeUrl?.();
  if (executionResumeUrl) {
    return executionResumeUrl;
  }

  const webhookUrl = extended.getNodeWebhookUrl?.('default');
  if (webhookUrl) {
    return webhookUrl;
  }

  const instanceBaseUrl = normalizeBaseUrl(context.getInstanceBaseUrl());
  const executionId = context.getExecutionId();
  return `${instanceBaseUrl}/webhook-waiting/${executionId}`;
}

export class H2l implements INodeType {
  description: INodeTypeDescription = {
    displayName: 'h2l',
    name: 'h2l',
    icon: 'file:h2l.svg',
    group: ['transform'],
    version: 1,
    subtitle: '={{$parameter["operation"]}}',
    description: 'Human-in-the-Loop forms and workflow tasks with h2l',
    defaults: {
      name: 'h2l',
    },
    inputs: ['main'] as NodeConnectionType[],
    outputs: ['main', 'main', 'main'] as NodeConnectionType[],
    outputNames: ['Completed', 'Expired', 'Cancelled'],
    credentials: [
      {
        name: 'h2lApi',
        required: true,
      },
    ],
    webhooks: [
      {
        name: 'default',
        httpMethod: 'POST',
        responseMode: 'onReceived',
        path: '',
        restartWebhook: true,
      },
    ],
    properties: [
      {
        displayName: 'Operation',
        name: 'operation',
        type: 'options',
        noDataExpression: true,
        options: [
          {
            name: 'Get Sensitive Data',
            value: 'getSensitiveData',
            description: 'Retrieve decrypted sensitive fields via Claim-Check (logged in h2l audit log)',
            action: 'Retrieve sensitive form data via claim check',
          },
          {
            name: 'Wait for Human Form',
            value: 'waitForHumanForm',
            description: 'Create an h2l human task and wait until it is completed, expired, or cancelled',
            action: 'Wait for a human form submission',
          },
        ],
        default: 'waitForHumanForm',
      },
      {
        displayName: 'Connector ID',
        name: 'connectorId',
        type: 'string',
        default: '',
        placeholder: '0193f1a0-7c4b-7000-8000-000000000001',
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description: 'Optional h2l Connector UUID override. Uses the Default Connector ID from credentials when empty.',
      },
      {
        displayName: 'Form Name or ID',
        name: 'formId',
        type: 'options',
        typeOptions: {
          loadOptionsMethod: 'getForms',
        },
        default: '',
        required: true,
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description:
          'Choose from the list, or specify an ID using an <a href="https://docs.n8n.io/code/expressions/">expression</a>',
      },
      {
        displayName: 'Form Version',
        name: 'formVersion',
        type: 'number',
        typeOptions: {
          minValue: 0,
        },
        default: 0,
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description: 'Optional published form version number. Leave at 0 to use the latest published version.',
      },
      {
        displayName: 'Assignee',
        name: 'assignee',
        type: 'string',
        default: '',
        placeholder: 'user@example.com',
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description: 'Optional user email or external ID to assign the task directly',
      },
      {
        displayName: 'Candidate Group Names or Codes',
        name: 'candidateGroups',
        type: 'multiOptions',
        typeOptions: {
          loadOptionsMethod: 'getGroups',
        },
        default: [],
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description:
          'Choose from the list, or specify IDs using an <a href="https://docs.n8n.io/code/expressions/">expression</a>',
      },
      {
        displayName: 'Task Timeout (ISO-8601)',
        name: 'timeoutIso8601',
        type: 'string',
        default: '',
        placeholder: 'P3D',
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description:
          'Optional ISO-8601 duration for h2l task expiration (e.g. PT4H, P3D). Ensure n8n workflow execution timeout is disabled or longer than h2l taskTimeout so the expired callback can resume the workflow.',
      },
      {
        displayName: 'Sensitive Data Mode',
        name: 'sensitiveDataMode',
        type: 'options',
        options: [
          {
            name: 'Claim-Check (Privacy by Default)',
            value: 'ClaimCheck',
            description:
              'Recommended. Keeps sensitive fields out of n8n execution history and returns a submissionId reference.',
          },
          {
            name: 'Pass-Through (GDPR Warning)',
            value: 'PassThrough',
            description:
              'GDPR Warning: Sends sensitive form fields in plain-text into n8n execution history. Use only with explicit opt-in.',
          },
        ],
        default: 'ClaimCheck',
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description: 'Controls whether fields marked isSensitive in h2l are protected via Claim-Check or passed through',
      },
      {
        displayName: 'Prefilled Data (JSON)',
        name: 'prefilledData',
        type: 'json',
        default: '{}',
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description: 'Optional JSON object of prefilled form values keyed by variableKey',
      },
      {
        displayName: 'External Task ID',
        name: 'externalTaskId',
        type: 'string',
        default: '',
        displayOptions: {
          show: {
            operation: ['waitForHumanForm'],
          },
        },
        description:
          'Optional deterministic task identifier for idempotency. Defaults to n8n execution ID and node name.',
      },
      {
        displayName: 'Submission ID',
        name: 'submissionId',
        type: 'string',
        default: '',
        placeholder: '={{$json._h2l.submissionId}}',
        required: true,
        displayOptions: {
          show: {
            operation: ['getSensitiveData'],
          },
        },
        description: 'Submission UUID returned in _h2l.submissionId by Wait for Human Form',
      },
    ],
  };

  methods = {
    loadOptions: {
      async getForms(this: ILoadOptionsFunctions): Promise<INodePropertyOptions[]> {
        const credentials = (await this.getCredentials('h2lApi')) as unknown as H2lCredentials;
        const baseUrl = normalizeBaseUrl(credentials.baseUrl);
        const options: INodePropertyOptions[] = [];

        const projects = (await this.helpers.httpRequestWithAuthentication.call(this, 'h2lApi', {
          method: 'GET',
          url: `${baseUrl}/api/v1/projects`,
          json: true,
        })) as H2lProject[];

        if (!Array.isArray(projects)) {
          return options;
        }

        for (const project of projects) {
          const processes = (await this.helpers.httpRequestWithAuthentication.call(this, 'h2lApi', {
            method: 'GET',
            url: `${baseUrl}/api/v1/projects/${encodeURIComponent(project.id)}/processes`,
            json: true,
          })) as H2lProcess[];

          if (!Array.isArray(processes)) {
            continue;
          }

          for (const process of processes) {
            const forms = (await this.helpers.httpRequestWithAuthentication.call(this, 'h2lApi', {
              method: 'GET',
              url: `${baseUrl}/api/v1/processes/${encodeURIComponent(process.id)}/forms`,
              json: true,
            })) as H2lFormDefinition[];

            if (!Array.isArray(forms)) {
              continue;
            }

            for (const form of forms) {
              options.push({
                name: `${project.name} / ${process.name} / ${form.name} (${form.stepKey})`,
                value: form.id,
              });
            }
          }
        }

        return options.sort((a, b) => a.name.localeCompare(b.name));
      },

      async getGroups(this: ILoadOptionsFunctions): Promise<INodePropertyOptions[]> {
        const credentials = (await this.getCredentials('h2lApi')) as unknown as H2lCredentials;
        const baseUrl = normalizeBaseUrl(credentials.baseUrl);

        const groups = (await this.helpers.httpRequestWithAuthentication.call(this, 'h2lApi', {
          method: 'GET',
          url: `${baseUrl}/api/v1/groups`,
          json: true,
        })) as H2lGroup[];

        if (!Array.isArray(groups)) {
          return [];
        }

        return groups
          .map((group) => ({
            name: `${group.name} (${group.code})`,
            value: group.code,
          }))
          .sort((a, b) => a.name.localeCompare(b.name));
      },
    },
  };

  async execute(this: IExecuteFunctions): Promise<INodeExecutionData[][]> {
    const items = this.getInputData();
    const operation = this.getNodeParameter('operation', 0) as string;
    const credentials = (await this.getCredentials('h2lApi')) as unknown as H2lCredentials;
    const baseUrl = normalizeBaseUrl(credentials.baseUrl);

    if (operation === 'getSensitiveData') {
      const results: INodeExecutionData[] = [];

      for (let itemIndex = 0; itemIndex < items.length; itemIndex++) {
        const submissionId = (this.getNodeParameter('submissionId', itemIndex) as string).trim();
        if (!submissionId) {
          throw new NodeOperationError(this.getNode(), 'Submission ID is required', { itemIndex });
        }

        const response = (await this.helpers.httpRequestWithAuthentication.call(this, 'h2lApi', {
          method: 'GET',
          url: `${baseUrl}/api/v1/submissions/${encodeURIComponent(submissionId)}/sensitive-data`,
          json: true,
        })) as IDataObject;

        results.push({
          json: response,
          pairedItem: { item: itemIndex },
        });
      }

      return [results, [], []];
    }

    if (operation === 'waitForHumanForm') {
      const connectorIdParam = (this.getNodeParameter('connectorId', 0, '') as string).trim();
      const connectorId = connectorIdParam || credentials.connectorId?.trim() || '';
      if (!connectorId) {
        throw new NodeOperationError(
          this.getNode(),
          'Connector ID is required either on the node or in h2l API credentials',
        );
      }

      const formId = (this.getNodeParameter('formId', 0) as string).trim();
      const formVersionParam = this.getNodeParameter('formVersion', 0, 0) as number;
      const assignee = this.getNodeParameter('assignee', 0, '') as string;
      const candidateGroups = this.getNodeParameter('candidateGroups', 0, []) as string[] | string;
      const timeoutIso8601 = this.getNodeParameter('timeoutIso8601', 0, '') as string;
      const sensitiveDataMode = this.getNodeParameter(
        'sensitiveDataMode',
        0,
        'ClaimCheck',
      ) as SensitiveDataMode;
      const prefilledData = this.getNodeParameter('prefilledData', 0, '{}');
      const customExternalTaskId = (
        this.getNodeParameter('externalTaskId', 0, '') as string
      ).trim();

      const externalTaskId =
        customExternalTaskId || `n8n:${this.getExecutionId()}:${this.getNode().name}`;
      const resumeUrl = resolveResumeUrl(this);

      let payload: IDataObject;
      try {
        payload = buildCreateTaskPayload({
          externalTaskId,
          formId,
          formVersion: formVersionParam > 0 ? formVersionParam : undefined,
          assignee,
          candidateGroups,
          prefilledData,
          resumeUrl,
          timeoutIso8601,
          sensitiveDataMode,
        });
      } catch (error) {
        throw new NodeOperationError(
          this.getNode(),
          error instanceof Error ? error.message : 'Invalid task parameters',
        );
      }

      const rawBody = JSON.stringify(payload);
      const headers: IDataObject = {
        'Content-Type': 'application/json',
      };

      const webhookSecret = credentials.webhookSecret?.trim();
      if (webhookSecret) {
        const timestamp = String(Math.floor(Date.now() / 1000));
        headers['X-H2L-Timestamp'] = timestamp;
        headers['X-H2L-Signature'] = computeHmacSignature(webhookSecret, timestamp, rawBody);
      }

      await this.helpers.httpRequestWithAuthentication.call(this, 'h2lApi', {
        method: 'POST',
        url: `${baseUrl}/api/v1/webhooks/connectors/${encodeURIComponent(connectorId)}/tasks`,
        headers,
        body: rawBody,
      });

      await this.putExecutionToWait(WAIT_TILL_INDEFINITE);

      return [items, [], []];
    }

    throw new NodeOperationError(this.getNode(), `Unsupported operation: ${operation}`);
  }

  async webhook(this: IWebhookFunctions): Promise<IWebhookResponseData> {
    const req = this.getRequestObject() as unknown as RawBodyRequest;
    const res = this.getResponseObject() as unknown as JsonResponse;
    const body = this.getBodyData();

    const rawBody =
      typeof req.rawBody === 'string'
        ? req.rawBody
        : Buffer.isBuffer(req.rawBody)
          ? req.rawBody.toString('utf8')
          : JSON.stringify(body);

    const credentials = (await this.getCredentials('h2lApi')) as unknown as H2lCredentials;
    const verification = verifyWebhookRequest(req.headers ?? {}, rawBody, {
      webhookSecret: credentials.webhookSecret,
      apiKey: credentials.apiKey,
    });

    if (!verification.valid) {
      res.status(401).json({
        error: 'Unauthorized',
        message: verification.reason,
      });
      return { noWebhookResponse: true };
    }

    try {
      const payload = parseWebhookPayload(body);
      const workflowData = routeWebhookPayload(payload);
      return { workflowData };
    } catch (error) {
      res.status(400).json({
        error: 'Bad Request',
        message: error instanceof Error ? error.message : 'Invalid webhook payload',
      });
      return { noWebhookResponse: true };
    }
  }
}
