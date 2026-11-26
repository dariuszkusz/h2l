import type {
  IAuthenticateGeneric,
  ICredentialTestRequest,
  ICredentialType,
  INodeProperties,
} from 'n8n-workflow';

export class H2lApi implements ICredentialType {
  name = 'h2lApi';

  displayName = 'H2l API';

  documentationUrl = 'https://docs.h2l.io/integrations/n8n';

  properties: INodeProperties[] = [
    {
      displayName: 'Base URL',
      name: 'baseUrl',
      type: 'string',
      default: '',
      placeholder: 'https://h2l.example.com',
      required: true,
      description: 'Base URL of your h2l instance without a trailing slash',
    },
    {
      displayName: 'API Key',
      name: 'apiKey',
      type: 'string',
      typeOptions: {
        password: true,
      },
      default: '',
      required: true,
      description: 'Bearer token or API key used to authenticate requests to the h2l API',
    },
    {
      displayName: 'Webhook Secret',
      name: 'webhookSecret',
      type: 'string',
      typeOptions: {
        password: true,
      },
      default: '',
      description:
        'Optional HMAC-SHA256 shared secret for signing outbound webhooks and verifying incoming h2l callbacks',
    },
    {
      displayName: 'Default Connector ID',
      name: 'connectorId',
      type: 'string',
      default: '',
      placeholder: '0193f1a0-7c4b-7000-8000-000000000001',
      description: 'Default h2l Connector UUID used when not overridden on the node',
    },
  ];

  authenticate: IAuthenticateGeneric = {
    type: 'generic',
    properties: {
      headers: {
        Authorization: '=Bearer {{$credentials.apiKey}}',
        'X-API-Key': '={{$credentials.apiKey}}',
      },
    },
  };

  test: ICredentialTestRequest = {
    request: {
      baseURL: '={{$credentials.baseUrl.replace(/\\/+$/, "")}}',
      url: '/api/v1/connectors',
      method: 'GET',
    },
  };
}
