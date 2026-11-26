import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
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

describe('Tier 1 — F7: 14-Day Grace Period & Soft Degradation (US-07.2)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T1-F7-01: displays 14-day Grace Period warning banner on /settings with expiry info', () => {
    const state = createMockState({
      preset: 'grace-period',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus').then((interception) => {
      expect(interception.response?.body).to.deep.include({
        status: 'GracePeriod',
        inGracePeriod: true,
      });
      expect(interception.response?.body.gracePeriodEndsAt).to.be.a('string');
    });

    byLiveRegion(/Grace|Okres przejściowy|Kulanzfrist|karenc/i).should('be.visible');
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'en' });
  });

  it('T1-F7-02: allows creating a new Active WorkflowProcess during active Grace Period', () => {
    const state = createMockState({
      preset: 'grace-period',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Grace Process');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'grace-process');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createProcess').its('response.statusCode').should('eq', 201);
    cy.contains('Grace Process').should('be.visible');
  });

  it('T1-F7-03: allows Admin user & group management during GracePeriod', () => {
    const state = createMockState({
      preset: 'grace-period',
      smtpConfigured: false,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'grace.user@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('EndUser');
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    cy.wait('@createUser').its('response.statusCode').should('eq', 201);
    cy.contains('grace.user@example.com').should('be.visible');
  });

  it('T1-F7-04: displays Expired soft-degradation banner while keeping license renewal enabled', () => {
    const state = createMockState({
      preset: 'expired',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus').then((interception) => {
      expect(interception.response?.body).to.deep.include({
        status: 'Expired',
        inGracePeriod: false,
      });
    });

    byAlert(/Expired|Wygasł|Abgelaufen/i).should('be.visible');
    byLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i).first().should('not.be.disabled');
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'en' });
  });

  it('T1-F7-05: allows viewing existing Projects, Processes, and Draft Forms while Expired', () => {
    const state = createMockState({
      preset: 'expired',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects').its('response.statusCode').should('eq', 200);
    cy.contains('HR').click();
    cy.wait('@listProcesses').its('response.statusCode').should('eq', 200);
    cy.contains('Software Purchase').click();
    cy.wait('@listForms').its('response.statusCode').should('eq', 200);
    cy.contains('Initial Request').should('be.visible');
  });

  it('T1-F7-06: recovers immediately from Expired to Active upon pasting renewed Ed25519 key', () => {
    const state = createMockState({ preset: 'expired' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.renewedPro);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();

    cy.wait('@activateLicense').then((interception) => {
      expect(interception.response?.statusCode).to.eq(200);
      expect(interception.response?.body).to.deep.include({
        status: 'Active',
        inGracePeriod: false,
        plan: 'Pro',
      });
    });

    cy.contains(/Active/i).should('be.visible');
    byLiveRegion().should('exist');
  });
});
