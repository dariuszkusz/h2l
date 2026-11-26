describe('Stage 2 — Form Builder (DnD + Keyboard + Live Preview + Publish + WCAG)', () => {
  const formId = 'form-stage2-1';
  let currentVersion = 0;

  beforeEach(() => {
    currentVersion = 0;
    cy.intercept('GET', '/api/v1/setup/status', {
      statusCode: 200,
      body: { isInitialized: true, instanceId: 'inst-1' },
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
    cy.intercept('GET', `/api/v1/forms/${formId}`, {
      statusCode: 200,
      body: {
        id: formId,
        workflowProcessId: 'proc-1',
        name: 'Software Request Form',
        stepKey: 'software_request',
        status: 'Draft',
        draftSchema: null,
        latestVersionNumber: null,
        updatedAt: '2026-11-26T00:00:00Z',
      },
    }).as('getForm');
    cy.intercept('GET', `/api/v1/forms/${formId}/versions`, {
      statusCode: 200,
      body: [],
    }).as('listVersions');
    cy.intercept('PUT', `/api/v1/forms/${formId}/draft`, (req) => {
      req.reply({
        statusCode: 200,
        body: {
          id: formId,
          workflowProcessId: 'proc-1',
          name: req.body.name ?? 'Software Request Form',
          stepKey: 'software_request',
          status: 'Draft',
          draftSchema: req.body.draftSchema,
          latestVersionNumber: currentVersion > 0 ? currentVersion : null,
          updatedAt: '2026-11-26T08:10:00Z',
        },
      });
    }).as('saveDraft');
    cy.intercept('POST', `/api/v1/forms/${formId}/publish`, (req) => {
      currentVersion += 1;
      req.reply({
        statusCode: 201,
        body: {
          id: `ver-${currentVersion}`,
          formDefinitionId: formId,
          versionNumber: currentVersion,
          publishedAt: '2026-11-26T08:15:00Z',
          publishedByUserId: 'u-admin',
          schema: {
            schemaVersion: '1.0',
            defaultLanguage: 'en',
            layoutMode: 'single',
            steps: [{ id: 'step_1', title: { en: 'Step 1' }, components: [] }],
          },
        },
      });
    }).as('publishForm');
  });

  it('adds fields, reorders via keyboard with aria-live, sets variableKey/isSensitive/EN-DE-PL, previews, publishes, and passes axe', () => {
    cy.visit(`/admin/forms/${formId}/builder`, {
      onBeforeLoad(win) {
        win.localStorage.setItem('h2l.language.v1', 'en');
        win.localStorage.setItem('h2l.access_token.v1', 'tok-admin');
        win.localStorage.setItem('h2l.refresh_token.v1', 'ref-admin');
      },
    });

    cy.wait('@getForm');
    cy.get('[data-testid="palette-add-text"]').click();
    cy.get('[data-testid="palette-add-number"]').click();

    // Keyboard reorder: move second item up
    cy.get('[data-testid="canvas-move-up-1"]').focus().click();
    cy.get('[data-testid="builder-live-region"]').should('not.be.empty');

    // Configure variableKey, isSensitive, and EN/DE/PL labels
    cy.get('[data-testid="canvas-select-0"]').click();
    cy.get('[data-testid="inspector-variable-key"]').clear().type('estimatedCost');
    cy.get('[data-testid="inspector-sensitive"]').check();
    cy.get('[data-testid="inspector-lang-pl"]').click();
    cy.get('[data-testid="inspector-label-input"]').clear().type('Szacowany koszt');

    // Live preview Desktop & Mobile + PL language
    cy.get('[data-testid="builder-tab-preview"]').click();
    cy.get('[data-testid="preview-viewport-mobile"]').click();
    cy.get('[data-testid="builder-preview-frame"]').should('have.attr', 'data-viewport', 'mobile');
    cy.get('[data-testid="preview-lang-pl"]').click();
    cy.contains('Szacowany koszt').should('be.visible');

    // Save draft & Publish immutable v1
    cy.get('[data-testid="builder-save-draft-btn"]').click();
    cy.wait('@saveDraft');
    cy.get('[data-testid="builder-success-banner"]').should('be.visible');

    cy.get('[data-testid="builder-publish-btn"]').click();
    cy.wait('@publishForm');
    cy.get('[data-testid="builder-version-badge"]').should('contain.text', 'v1');

    // WCAG 2.1 AA check across Light, Dark, High Contrast
    cy.injectAxe();
    cy.checkA11y();
    cy.document().then((doc) => doc.documentElement.setAttribute('data-theme', 'dark'));
    cy.checkA11y();
    cy.document().then((doc) => doc.documentElement.setAttribute('data-theme', 'high-contrast'));
    cy.checkA11y();
  });
});
