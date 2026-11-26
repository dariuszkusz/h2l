import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  DEFAULT_END_USER_CREDENTIALS,
  MOCK_IDS,
  VALID_LICENSE_KEYS,
} from '../mocks/mock-api.state';
import {
  assertInvalidField,
  byAlert,
  byButton,
  byLabel,
  byLink,
  fillByLabel,
} from '../support/a11y-queries';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 2 — F6 Boundaries: Free 402 Caps, Bad Signature, InstanceId Mismatch & RBAC (US-07.1)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T2-F6-01: blocks inviting a 2nd Admin on Free plan with 402 ProblemDetails and upgrade link', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'second.admin@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('Admin');
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    cy.wait('@createUser').its('response.statusCode').should('eq', 402);
    byAlert().should('be.visible').and('contain.text', MOCK_IDS.instanceId);
    byLink(/Settings|License|Ustawienia|Licencj|Einstellungen|Lizenz/i).should('exist');
  });

  it('T2-F6-02: blocks creating a 2nd Active WorkflowProcess on Free plan with 402 ProblemDetails', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Extra Process');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'extra-process');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createProcess').its('response.statusCode').should('eq', 402);
    byAlert().should('be.visible');
  });

  it('T2-F6-03: rejects invalid Ed25519 signature (400 ProblemDetails) while keeping Free plan unchanged', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.invalidSignature);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();

    cy.wait('@activateLicense').its('response.statusCode').should('eq', 400);
    byAlert().should('be.visible');
    cy.contains(/Free/i).should('be.visible');
  });

  it('T2-F6-04: rejects valid key bound to another InstanceId (400) and keeps current InstanceId visible', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.wrongInstance);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();

    cy.wait('@activateLicense').its('response.statusCode').should('eq', 400);
    byAlert().should('be.visible').and('contain.text', MOCK_IDS.instanceId);
  });

  it('T2-F6-05: rejects empty or whitespace-only licenseKey client-side without calling POST /licensing/activate', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, '   ');
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();

    assertInvalidField(/License.*Key|Klucz licencji|Lizenzschlüssel/i);
    cy.get('@activateLicense.all').should('have.length', 0);
  });

  it('T2-F6-06: ensures EndUser preferences panel remains functional without calling licensing endpoints', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
      endUserLanguage: 'en',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_END_USER_CREDENTIALS.email, DEFAULT_END_USER_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    byButton(/Polski/i).first().click();
    cy.wait('@updatePreferences').its('response.statusCode').should('eq', 204);
    cy.get('@getLicensingStatus.all').should('have.length', 0);
    cy.get('@activateLicense.all').should('have.length', 0);
  });
});
