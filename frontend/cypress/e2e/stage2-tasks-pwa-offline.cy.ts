describe('Stage 2 — End-User Tasks PWA Portal, Claim/Unclaim, 409 Conflict & Offline Sync', () => {
  const taskId = 'task-stage2-1';

  const sampleTaskDetail = {
    id: taskId,
    status: 'InProgress',
    formVersionId: 'fv-1',
    formVersionNumber: 1,
    schema: {
      schemaVersion: '1.0',
      defaultLanguage: 'en',
      layoutMode: 'single',
      steps: [
        {
          id: 'step_1',
          title: { en: 'Approval', de: 'Freigabe', pl: 'Akceptacja' },
          components: [
            {
              id: 'cmp_note',
              type: 'text',
              variableKey: 'decisionNote',
              label: { en: 'Decision note', de: 'Entscheidungsnotiz', pl: 'Komentarz decyzji' },
              validators: [{ type: 'required' }],
            },
          ],
        },
      ],
    },
    prefilledData: {},
    assignedUserId: 'u-end',
    claimedByUserId: 'u-end',
    canClaim: false,
    canUnclaim: true,
    sensitiveDataMode: 'ClaimCheck',
    expiresAt: null,
    createdAt: '2026-11-26T08:00:00Z',
    completedAt: null,
  };

  beforeEach(() => {
    cy.intercept('GET', '/api/v1/setup/status', {
      statusCode: 200,
      body: { isInitialized: true, instanceId: 'inst-1' },
    });
    cy.intercept('GET', '/api/v1/license/status', {
      statusCode: 200,
      body: {
        tier: 'Free',
        status: 'Active',
        maxActiveProcesses: 1,
        currentActiveProcesses: 1,
        gracePeriodEndsAt: null,
        expiresAt: null,
      },
    });
    cy.intercept('POST', '/hubs/tasks/negotiate*', {
      statusCode: 200,
      body: {},
    });
    cy.intercept('GET', '/api/v1/users/me', {
      statusCode: 200,
      body: {
        id: 'u-end',
        email: 'user@h2l.local',
        systemRole: 'EndUser',
        preferredLanguage: 'en',
        preferredTheme: 'light',
        isActive: true,
        createdAt: '2026-11-26T00:00:00Z',
      },
    });
    cy.intercept('GET', '/api/v1/tasks*', {
      statusCode: 200,
      body: {
        items: [
          {
            id: taskId,
            formDefinitionId: 'f1',
            formVersionNumber: 1,
            formName: 'Purchase Approval',
            processName: 'Procurement',
            projectName: 'IT',
            status: 'Pending',
            assignedUserId: null,
            claimedByUserId: null,
            canClaim: true,
            canUnclaim: false,
            expiresAt: null,
            createdAt: '2026-11-26T08:00:00Z',
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      },
    }).as('listTasks');
  });

  it('handles 409 TASK_ALREADY_CLAIMED with localized error and refreshes task list', () => {
    cy.intercept('POST', `/api/v1/tasks/${taskId}/claim`, {
      statusCode: 409,
      headers: { 'content-type': 'application/problem+json' },
      body: {
        status: 409,
        title: 'Task already claimed',
        errorCode: 'TASK_ALREADY_CLAIMED',
      },
    }).as('claimConflict');

    cy.visit('/tasks', {
      onBeforeLoad(win) {
        win.localStorage.setItem('h2l.language.v1', 'en');
        win.sessionStorage.setItem('h2l.auth.accessToken', 'tok-end');
        win.sessionStorage.setItem('h2l.auth.refreshToken', 'ref-end');
      },
    });

    cy.wait('@listTasks');
    cy.get(`[data-testid="task-claim-btn-${taskId}"]`).click();
    cy.wait('@claimConflict');
    cy.wait('@listTasks');
    cy.get('[data-testid="tasks-error-banner"]').should('contain.text', 'already been claimed');

    cy.injectAxe();
    cy.checkA11y();
  });

  it('supports task detail Unclaim and online Submit with Idempotency-Key header', () => {
    cy.intercept('GET', `/api/v1/tasks/${taskId}`, {
      statusCode: 200,
      body: sampleTaskDetail,
    }).as('getTaskDetail');
    cy.intercept('POST', `/api/v1/tasks/${taskId}/unclaim`, {
      statusCode: 200,
      body: { ...sampleTaskDetail, status: 'Pending', canClaim: true, canUnclaim: false },
    }).as('unclaimTask');
    cy.intercept('POST', `/api/v1/tasks/${taskId}/submit`, (req) => {
      expect(req.headers['idempotency-key']).to.be.a('string').and.not.be.empty;
      req.reply({
        statusCode: 200,
        body: {
          taskId,
          submissionId: 'sub-1',
          status: 'Completed',
          submittedAt: '2026-11-26T08:30:00Z',
        },
      });
    }).as('submitTask');

    cy.visit(`/tasks/${taskId}`, {
      onBeforeLoad(win) {
        win.localStorage.setItem('h2l.language.v1', 'en');
        win.sessionStorage.setItem('h2l.auth.accessToken', 'tok-end');
        win.sessionStorage.setItem('h2l.auth.refreshToken', 'ref-end');
      },
    });

    cy.wait('@getTaskDetail');
    cy.get('[data-testid="task-detail-unclaim-btn"]').should('be.visible');
    cy.get('[data-testid="field-decisionNote"]').type('Approved for Q4');
    cy.get('[data-testid="form-renderer-submit-btn"]').click();
    cy.wait('@submitTask');
    cy.get('[data-testid="task-detail-success-banner"]').should('be.visible');

    cy.injectAxe();
    cy.checkA11y();
  });
});
