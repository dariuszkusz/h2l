describe('Stage 3 — Connectors, Dead-Letter, NeedsAssignment, Workflow Templates & WCAG', () => {
  const formId = 'form-stage3-1';
  const projectId = 'proj-stage3-1';
  const processId = 'proc-stage3-1';

  let connectorsList: Array<Record<string, unknown>>;
  let deadLettersList: Array<Record<string, unknown>>;
  let needsAssignmentList: Array<Record<string, unknown>>;

  beforeEach(() => {
    connectorsList = [
      {
        id: 'conn-camunda-1',
        name: 'Camunda 8 Self-Managed',
        connectorType: 'Camunda8',
        baseUrl: 'https://camunda.internal:8080',
        authType: 'OidcClientCredentials',
        hasCredentials: true,
        hasCustomCaCertificate: true,
        callbackUrl: null,
        isActive: true,
        createdAt: '2026-11-26T08:00:00Z',
      },
    ];
    deadLettersList = [
      {
        id: 'outbox-dl-1',
        humanTaskId: 'task-dl-1',
        formSubmissionId: 'sub-dl-1',
        connectorName: 'n8n Self-Hosted',
        messageType: 'CompleteTaskCallback',
        retryCount: 5,
        deadLetterReason: 'HTTP 404 Not Found',
        createdAt: '2026-11-26T08:15:00Z',
      },
    ];
    needsAssignmentList = [
      {
        id: 'task-na-1',
        formDefinitionId: formId,
        formVersionNumber: 1,
        formName: 'Software Purchase Request',
        processName: 'Procurement',
        projectName: 'IT',
        status: 'NeedsAssignment',
        assignedUserId: null,
        claimedByUserId: null,
        canClaim: false,
        canUnclaim: false,
        expiresAt: null,
        createdAt: '2026-11-26T08:20:00Z',
      },
    ];

    cy.intercept('GET', '/api/v1/setup/status', {
      statusCode: 200,
      body: { isInitialized: true, instanceId: 'inst-stage3' },
    });
    cy.intercept('GET', '/api/v1/users/me', {
      statusCode: 200,
      body: {
        id: 'u-admin',
        email: 'admin@h2l.local',
        systemRole: 'Admin',
        preferredLanguage: 'en',
        preferredTheme: 'light',
        isActive: true,
        createdAt: '2026-11-26T00:00:00Z',
      },
    });
    cy.intercept('GET', '/api/v1/users', {
      statusCode: 200,
      body: [
        {
          id: 'u-admin',
          email: 'admin@h2l.local',
          systemRole: 'Admin',
          preferredLanguage: 'en',
          preferredTheme: 'light',
          isActive: true,
          groupIds: [],
          createdAt: '2026-11-26T00:00:00Z',
        },
        {
          id: 'u-worker',
          email: 'worker@h2l.local',
          systemRole: 'EndUser',
          preferredLanguage: 'pl',
          preferredTheme: 'light',
          isActive: true,
          groupIds: ['grp-it'],
          createdAt: '2026-11-26T00:00:00Z',
        },
      ],
    }).as('listUsers');
    cy.intercept('GET', '/api/v1/groups', {
      statusCode: 200,
      body: [{ id: 'grp-it', name: 'IT', code: 'IT', memberCount: 1, createdAt: '2026-11-26T00:00:00Z' }],
    });

    cy.intercept('GET', '/api/v1/connectors', (req) => {
      req.reply({ statusCode: 200, body: { items: connectorsList } });
    }).as('listConnectors');

    cy.intercept('POST', '/api/v1/connectors', (req) => {
      if (req.body.isActive && connectorsList.some((c) => c['isActive'] === true)) {
        req.reply({
          statusCode: 402,
          headers: { 'content-type': 'application/problem+json' },
          body: {
            status: 402,
            errorCode: 'FREE_PLAN_CONNECTOR_LIMIT_EXCEEDED',
            detail: 'Free plan allows at most 1 active connector.',
          },
        });
        return;
      }
      const created = {
        id: 'conn-n8n-2',
        name: req.body.name,
        connectorType: req.body.connectorType,
        baseUrl: req.body.baseUrl,
        authType: req.body.authType,
        hasCredentials: Boolean(req.body.credentials),
        hasCustomCaCertificate: Boolean(req.body.customCaCertificatePem),
        callbackUrl: req.body.callbackUrl ?? null,
        isActive: req.body.isActive,
        createdAt: '2026-11-26T08:30:00Z',
      };
      connectorsList = [created, ...connectorsList];
      req.reply({ statusCode: 201, body: created });
    }).as('createConnector');

    cy.intercept('PUT', '/api/v1/connectors/*', (req) => {
      const id = req.url.split('/').pop() ?? '';
      const updated = {
        id,
        name: req.body.name,
        connectorType: req.body.connectorType,
        baseUrl: req.body.baseUrl,
        authType: req.body.authType,
        hasCredentials: true,
        hasCustomCaCertificate: !req.body.clearCustomCaCertificate,
        callbackUrl: req.body.callbackUrl ?? null,
        isActive: req.body.isActive,
        createdAt: '2026-11-26T08:00:00Z',
      };
      connectorsList = connectorsList.map((c) => (c['id'] === id ? updated : c));
      req.reply({ statusCode: 200, body: updated });
    }).as('updateConnector');

    cy.intercept('DELETE', '/api/v1/connectors/*', (req) => {
      const id = req.url.split('/').pop() ?? '';
      connectorsList = connectorsList.filter((c) => c['id'] !== id);
      req.reply({ statusCode: 204 });
    }).as('deleteConnector');

    cy.intercept('POST', '/api/v1/connectors/*/test', {
      statusCode: 200,
      body: {
        isReachable: true,
        detectedEngine: 'Camunda 8',
        detectedVersion: '8.7.2',
        latencyMs: 31,
        errorMessage: null,
      },
    }).as('testConnector');

    cy.intercept('GET', '/api/v1/outbox/dead-letters', (req) => {
      req.reply({ statusCode: 200, body: { items: deadLettersList } });
    }).as('listDeadLetters');

    cy.intercept('POST', '/api/v1/outbox/dead-letters/*/retry', (req) => {
      const segments = req.url.split('/');
      const outboxId = segments[segments.length - 2];
      deadLettersList = deadLettersList.filter((d) => d['id'] !== outboxId);
      req.reply({ statusCode: 202 });
    }).as('retryDeadLetter');

    cy.intercept('GET', '/api/v1/tasks?status=NeedsAssignment*', (req) => {
      req.reply({
        statusCode: 200,
        body: {
          items: needsAssignmentList,
          page: 1,
          pageSize: 50,
          totalCount: needsAssignmentList.length,
          totalPages: 1,
        },
      });
    }).as('listNeedsAssignment');

    cy.intercept('POST', '/api/v1/tasks/*/assign', (req) => {
      const segments = req.url.split('/');
      const taskId = segments[segments.length - 2];
      needsAssignmentList = needsAssignmentList.filter((t) => t['id'] !== taskId);
      req.reply({
        statusCode: 200,
        body: { id: taskId, status: 'Pending', assignedUserId: req.body.userId },
      });
    }).as('assignTask');
  });

  function visitAsAdmin(path: string, lang: 'en' | 'de' | 'pl' = 'en'): void {
    cy.visit(path, {
      onBeforeLoad(win) {
        win.localStorage.setItem('h2l.language.v1', lang);
        win.sessionStorage.setItem('h2l.auth.accessToken', 'tok-admin');
        win.sessionStorage.setItem('h2l.auth.refreshToken', 'ref-admin');
      },
    });
  }

  it('handles Connectors CRUD, Test Connection, 402 Free plan limit, Dead-Letter Retry, NeedsAssignment, and WCAG across themes/languages', () => {
    visitAsAdmin('/admin/connectors', 'en');
    cy.wait('@listConnectors');

    // Verify existing connector badges without exposing secrets
    cy.get('[data-testid="connector-card-conn-camunda-1"]').should('be.visible');
    cy.get('[data-testid="connector-credentials-conn-camunda-1"]').should('contain.text', 'Credentials stored');
    cy.get('[data-testid="connector-custom-ca-conn-camunda-1"]').should('contain.text', 'Custom CA TLS');

    // Test Connection
    cy.get('[data-testid="test-connector-btn-conn-camunda-1"]').click();
    cy.wait('@testConnector');
    cy.get('[data-testid="connector-test-result-conn-camunda-1"]')
      .should('be.visible')
      .and('contain.text', 'Camunda 8')
      .and('contain.text', '8.7.2')
      .and('contain.text', '31 ms');

    // 402 Free plan 1 active connector limit -> shows upgrade link to /settings
    cy.get('[data-testid="open-create-connector-btn"]').click();
    cy.get('[data-testid="connector-dialog"]').should('be.visible');
    cy.get('[data-testid="connector-name-input"]').type('n8n Second Active');
    cy.get('[data-testid="connector-type-select"]').select('N8n');
    cy.get('[data-testid="connector-base-url-input"]').type('https://n8n.internal:5678');
    cy.get('[data-testid="connector-hmac-secret-input"]').type('super-secret-hmac');
    cy.get('[data-testid="submit-connector-btn"]').click();
    cy.wait('@createConnector');
    cy.get('[data-testid="connectors-error-banner"]').should('be.visible');
    cy.get('[data-testid="connectors-upgrade-link"]').should('have.attr', 'href', '/settings');

    // Create inactive n8n connector (succeeds under Free plan), edit it, then delete it
    cy.get('[data-testid="open-create-connector-btn"]').click();
    cy.get('[data-testid="connector-name-input"]').type('n8n Standby');
    cy.get('[data-testid="connector-type-select"]').select('N8n');
    cy.get('[data-testid="connector-base-url-input"]').type('https://n8n.standby.local');
    cy.get('[data-testid="connector-active-checkbox"]').uncheck();
    cy.get('[data-testid="submit-connector-btn"]').click();
    cy.wait('@createConnector');
    cy.get('[data-testid="connector-card-conn-n8n-2"]').should('be.visible');

    // Edit connector
    cy.get('[data-testid="edit-connector-btn-conn-n8n-2"]').click();
    cy.get('[data-testid="connector-name-input"]').clear().type('n8n Standby Updated');
    cy.get('[data-testid="submit-connector-btn"]').click();
    cy.wait('@updateConnector');
    cy.get('[data-testid="connector-card-conn-n8n-2"]').should('contain.text', 'n8n Standby Updated');

    // Delete connector
    cy.get('[data-testid="delete-connector-btn-conn-n8n-2"]').click();
    cy.get('[data-testid="delete-connector-dialog"]').should('be.visible');
    cy.get('[data-testid="confirm-delete-connector-btn"]').click();
    cy.wait('@deleteConnector');
    cy.get('[data-testid="connector-card-conn-n8n-2"]').should('not.exist');

    // Dead-Letter Queue Retry with aria-live confirmation
    cy.get('[data-testid="connectors-tab-dead-letters"]').click();
    cy.wait('@listDeadLetters');
    cy.get('[data-testid="dead-letter-item-outbox-dl-1"]').should('be.visible');
    cy.get('[data-testid="retry-dead-letter-btn-outbox-dl-1"]').click();
    cy.wait('@retryDeadLetter');
    cy.get('[data-testid="connectors-live-region"]').should('not.be.empty');
    cy.get('[data-testid="dead-letters-empty"]').should('be.visible');

    // NeedsAssignment tab reassignment
    cy.get('[data-testid="connectors-tab-needs-assignment"]').click();
    cy.wait('@listNeedsAssignment');
    cy.get('[data-testid="needs-assignment-item-task-na-1"]').should('be.visible');
    cy.get('[data-testid="assign-user-select-task-na-1"]').select('u-worker');
    cy.get('[data-testid="assign-task-btn-task-na-1"]').click();
    cy.wait('@assignTask');
    cy.get('[data-testid="connectors-live-region"]').should('not.be.empty');
    cy.get('[data-testid="needs-assignment-empty"]').should('be.visible');

    // WCAG 2.1 AA across Light, Dark, High Contrast
    cy.injectAxe();
    cy.checkA11y();
    cy.document().then((doc) => doc.documentElement.setAttribute('data-theme', 'dark'));
    cy.checkA11y();
    cy.document().then((doc) => doc.documentElement.setAttribute('data-theme', 'high-contrast'));
    cy.checkA11y();

    // Verify DE and PL i18n rendering + WCAG
    visitAsAdmin('/admin/connectors', 'de');
    cy.wait('@listConnectors');
    cy.get('[data-testid="admin-connectors-heading"]').should('contain.text', 'Workflow-Konnektoren');
    cy.injectAxe();
    cy.checkA11y();

    visitAsAdmin('/admin/connectors', 'pl');
    cy.wait('@listConnectors');
    cy.get('[data-testid="admin-connectors-heading"]').should('contain.text', 'Konektory workflow');
    cy.injectAxe();
    cy.checkA11y();
  });

  it('exports Camunda 8 Element Template and copies n8n JSON (both variants) on Form Builder and Projects', () => {
    cy.intercept('GET', `/api/v1/forms/${formId}`, {
      statusCode: 200,
      body: {
        id: formId,
        workflowProcessId: processId,
        name: 'Software Purchase Request',
        stepKey: 'software_purchase',
        status: 'Published',
        draftSchema: {
          schemaVersion: '1.0',
          defaultLanguage: 'en',
          layoutMode: 'single',
          steps: [{ id: 'step_1', title: { en: 'Step 1' }, components: [] }],
        },
        latestVersionNumber: 1,
        updatedAt: '2026-11-26T08:00:00Z',
      },
    }).as('getForm');
    cy.intercept('GET', `/api/v1/forms/${formId}/versions`, { statusCode: 200, body: [] });
    cy.intercept('GET', `/api/v1/forms/${formId}/templates/camunda-element-template`, {
      statusCode: 200,
      body: {
        $schema: 'https://unpkg.com/@camunda/zeebe-element-templates-json-schema/resources/schema.json',
        id: `h2l.${formId}`,
        name: 'h2l: Software Purchase Request',
      },
    }).as('camundaTemplate');
    cy.intercept('GET', `/api/v1/forms/${formId}/templates/n8n-nodes*`, (req) => {
      const variant = req.query['variant'] ?? 'community-node';
      req.reply({
        statusCode: 200,
        body: {
          variant,
          workflowJson: { nodes: [{ name: `h2l-${variant}` }], connections: {} },
        },
      });
    }).as('n8nTemplate');

    // 1. Form Builder page
    visitAsAdmin(`/admin/forms/${formId}/builder`, 'en');
    cy.wait('@getForm');

    cy.get('[data-testid="export-camunda-template-btn"]').click();
    cy.wait('@camundaTemplate');
    cy.get('[data-testid="template-export-confirmation"]').should('contain.text', 'h2l-camunda-template-software_purchase.json');

    // Community Node variant
    cy.get('[data-testid="export-n8n-variant-select"]').select('community-node');
    cy.get('[data-testid="export-n8n-copy-btn"]').click();
    cy.wait('@n8nTemplate').its('request.url').should('include', 'variant=community-node');
    cy.get('[data-testid="template-export-confirmation"]').should('contain.text', 'Community Node');

    // Fallback Wait Node variant
    cy.get('[data-testid="export-n8n-variant-select"]').select('fallback-wait-node');
    cy.get('[data-testid="export-n8n-copy-btn"]').click();
    cy.wait('@n8nTemplate').its('request.url').should('include', 'variant=fallback-wait-node');
    cy.get('[data-testid="template-export-confirmation"]').should('contain.text', 'Fallback Wait Node');

    // 2. Projects page
    cy.intercept('GET', '/api/v1/projects', {
      statusCode: 200,
      body: [{ id: projectId, name: 'IT', description: null, createdAt: '2026-11-26T00:00:00Z' }],
    }).as('listProjects');
    cy.intercept('GET', `/api/v1/projects/${projectId}/processes`, {
      statusCode: 200,
      body: [
        {
          id: processId,
          projectId,
          name: 'Procurement',
          externalProcessKey: 'procurement',
          status: 'Active',
          createdAt: '2026-11-26T00:00:00Z',
        },
      ],
    }).as('listProcesses');
    cy.intercept('GET', `/api/v1/processes/${processId}/forms`, {
      statusCode: 200,
      body: [
        {
          id: formId,
          workflowProcessId: processId,
          name: 'Software Purchase Request',
          stepKey: 'software_purchase',
          status: 'Published',
          updatedAt: '2026-11-26T08:00:00Z',
        },
      ],
    }).as('listForms');
    cy.intercept('GET', `/api/v1/forms/${formId}/access-rules`, {
      statusCode: 200,
      body: { formId, rules: [] },
    });

    visitAsAdmin('/admin/projects', 'en');
    cy.wait('@listProjects');
    cy.get(`[data-testid="project-item-${projectId}"]`).click();
    cy.wait('@listProcesses');
    cy.get('[data-testid="process-item-procurement"]').click();
    cy.wait('@listForms');
    cy.get('[data-testid="form-item-software_purchase"]').click();

    cy.get('[data-testid="workflow-template-export"]').should('be.visible');
    cy.get('[data-testid="export-camunda-template-btn"]').click();
    cy.wait('@camundaTemplate');
    cy.get('[data-testid="template-export-confirmation"]').should('be.visible');

    cy.injectAxe();
    cy.checkA11y();
  });
});
