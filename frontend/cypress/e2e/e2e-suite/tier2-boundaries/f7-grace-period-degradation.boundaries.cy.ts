import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  DEFAULT_END_USER_CREDENTIALS,
  VALID_LICENSE_KEYS,
} from '../mocks/mock-api.state';
import {
  byAlert,
  byButton,
  byLabel,
  byLink,
  byLiveRegion,
  fillByLabel,
} from '../support/a11y-queries';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 2 — F7 Boundaries: Day-14 Grace Boundary & Expired Soft Degradation (US-07.2)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T2-F7-01: keeps operations unblocked on Day-14 Grace Period boundary (< 24h remaining)', () => {
    const state = createMockState({
      preset: 'grace-period',
      withSeededProcess: true,
      graceHoursRemaining: 12,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus').its('response.body.inGracePeriod').should('eq', true);
    byLiveRegion().should('exist');

    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Day14 Grace Proc');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'day14-grace-proc');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createProcess').its('response.statusCode').should('eq', 201);
  });

  it('T2-F7-02: blocks creating new Active WorkflowProcess with 402 once license transitions to Expired', () => {
    const state = createMockState({
      preset: 'expired',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Blocked Expired Proc');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'blocked-expired-proc');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createProcess').its('response.statusCode').should('eq', 402);
    byAlert().should('be.visible');
    byLink(/Settings|License|Ustawienia|Licencj|Einstellungen|Lizenz/i).should('exist');
  });

  it('T2-F7-03: allows creating new Draft FormDefinitions even when license status is Expired', () => {
    const state = createMockState({
      preset: 'expired',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    cy.contains('Software Purchase').click();
    cy.wait('@listForms');

    byButton(/New Draft Form|New Form|Create Form|Add Form|Nowy formularz|Utwórz formularz|Neues Formular|Formular erstellen/i).first().click();
    fillByLabel(/Form.*Name|Nazwa formularza|Formularname/i, 'Expired Safe Draft');
    fillByLabel(/Step.*Key|Klucz kroku|Schrittschlüssel/i, 'expired-safe-draft');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createForm').its('response.statusCode').should('eq', 201);
    cy.contains('Expired Safe Draft').should('be.visible');
  });

  it('T2-F7-04: allows EndUser login and preference updates during Expired soft degradation', () => {
    const state = createMockState({
      preset: 'expired',
      withSeededUsers: true,
      endUserLanguage: 'en',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_END_USER_CREDENTIALS.email, DEFAULT_END_USER_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    byButton(/Deutsch/i).first().click();
    cy.wait('@updatePreferences').its('response.statusCode').should('eq', 204);
  });

  it('T2-F7-05: blocks 2nd Admin invite (402) during Expired while keeping existing users readable', () => {
    const state = createMockState({
      preset: 'expired',
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers').its('response.statusCode').should('eq', 200);
    cy.contains('user@example.com').should('be.visible');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'second.admin.expired@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('Admin');
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    cy.wait('@createUser').its('response.statusCode').should('eq', 402);
    byAlert().should('be.visible');
  });

  it('T2-F7-06: rejects already-expired renewal key with 400 ProblemDetails and keeps renewal form usable', () => {
    const state = createMockState({ preset: 'expired' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.expiredRenewal);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();

    cy.wait('@activateLicense').its('response.statusCode').should('eq', 400);
    byAlert().should('be.visible');
    byLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i).first().should('not.be.disabled');
  });
});
