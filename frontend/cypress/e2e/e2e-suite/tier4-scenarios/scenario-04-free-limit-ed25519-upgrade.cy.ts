import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  MOCK_IDS,
  VALID_LICENSE_KEYS,
} from '../mocks/mock-api.state';
import {
  byAlert,
  byButton,
  byLabel,
  byLiveRegion,
  fillByLabel,
} from '../support/a11y-queries';
import { injectAndCheckA11y } from '../support/axe-helpers';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 4 — T4-04: Free Plan Limit Hit -> Ed25519 Key Mistake -> Successful Upgrade & Unblock', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T4-04: hits 402 on process & admin, rejects mismatched InstanceId (400), activates valid Pro key, and unblocks both', () => {
    const state = createMockState({
      preset: 'initialized-free',
      adminLanguage: 'pl',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);

    // 1. Attempt 2nd Active Process -> 402
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    byButton(/Utwórz proces|New Process|Create Process/i).first().click();
    fillByLabel(/Nazwa procesu|Process.*Name/i, 'Second Process');
    fillByLabel(/Klucz procesu|External.*Key/i, 'second-process');
    byButton(/Zapisz|Utwórz|Save|Create/i).last().click();
    cy.wait('@createProcess').its('response.statusCode').should('eq', 402);
    byAlert().should('be.visible').and('contain.text', MOCK_IDS.instanceId);
    injectAndCheckA11y({ route: ROUTES.adminProjects, theme: 'light', language: 'pl' });

    // 2. Attempt 2nd Admin -> 402
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    byButton(/Zaproś|Invite|Add User/i).first().click();
    fillByLabel(/E-?Mail|Email/i, 'admin2@acme.pl');
    byLabel(/Rola|Role/i).first().select('Admin');
    byButton(/Wyślij|Zaproś|Send Invite|Invite/i).last().click();
    cy.wait('@createUser').its('response.statusCode').should('eq', 402);
    byAlert().should('be.visible');

    // 3. Wrong InstanceId key -> 400
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');
    fillByLabel(/Klucz licencji|License.*Key/i, VALID_LICENSE_KEYS.wrongInstance);
    byButton(/Aktywuj licencję|Activate.*License/i).first().click();
    cy.wait('@activateLicense').its('response.statusCode').should('eq', 400);
    byAlert().should('be.visible').and('contain.text', MOCK_IDS.instanceId);

    // 4. Valid Pro key -> 200
    fillByLabel(/Klucz licencji|License.*Key/i, VALID_LICENSE_KEYS.pro);
    byButton(/Aktywuj licencję|Activate.*License/i).first().click();
    cy.wait('@activateLicense').its('response.body.plan').should('eq', 'Pro');
    byLiveRegion().should('exist');
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'pl' });

    // 5. Unblocked: 2nd Active Process & 2nd Admin both succeed (201)
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    byButton(/Utwórz proces|New Process|Create Process/i).first().click();
    fillByLabel(/Nazwa procesu|Process.*Name/i, 'Second Process');
    fillByLabel(/Klucz procesu|External.*Key/i, 'second-process');
    byButton(/Zapisz|Utwórz|Save|Create/i).last().click();
    cy.wait('@createProcess').its('response.statusCode').should('eq', 201);

    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    byButton(/Zaproś|Invite|Add User/i).first().click();
    fillByLabel(/E-?Mail|Email/i, 'admin2@acme.pl');
    byLabel(/Rola|Role/i).first().select('Admin');
    byButton(/Wyślij|Zaproś|Send Invite|Invite/i).last().click();
    cy.wait('@createUser').its('response.statusCode').should('eq', 201);
  });
});
