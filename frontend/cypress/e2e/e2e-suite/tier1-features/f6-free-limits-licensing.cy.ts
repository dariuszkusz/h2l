import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  DEFAULT_END_USER_CREDENTIALS,
  MOCK_IDS,
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
import { injectAndCheckA11y } from '../support/axe-helpers';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 1 — F6: Free Plan Limits & Offline Ed25519 License Activation (US-07.1)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T1-F6-01: displays Free plan status, InstanceId, and usage vs limits on /settings', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');
    cy.wait('@getLicensingStatus').then((interception) => {
      expect(interception.response?.body).to.deep.include({
        instanceId: MOCK_IDS.instanceId,
        plan: 'Free',
        status: 'Active',
      });
    });

    cy.contains(MOCK_IDS.instanceId).should('be.visible');
    cy.contains(/Free/i).should('be.visible');
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'en' });
  });

  it('T1-F6-02: allows unlimited EndUser invitations under Free plan (maxEndUsers: null)', () => {
    const state = createMockState({
      preset: 'initialized-free',
      smtpConfigured: false,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');

    ['u1@example.com', 'u2@example.com'].forEach((email) => {
      byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
      fillByLabel(/Email|E-Mail/i, email);
      byLabel(/Role|Rola|Rolle/i).first().select('EndUser');
      byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();
      cy.wait('@createUser').its('response.statusCode').should('eq', 201);
    });
  });

  it('T1-F6-03: activates valid offline Ed25519 Pro license key from /settings', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.pro);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();

    cy.wait('@activateLicense').then((interception) => {
      expect(interception.request.body).to.deep.eq({ licenseKey: VALID_LICENSE_KEYS.pro });
      expect(interception.response?.statusCode).to.eq(200);
      expect(interception.response?.body).to.deep.include({
        plan: 'Pro',
        status: 'Active',
      });
    });

    cy.contains(/Pro/i).should('be.visible');
    byLiveRegion().should('exist');
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'en' });
  });

  it('T1-F6-04: activates Enterprise license key and displays Enterprise unlimited indicators', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.enterprise);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();

    cy.wait('@activateLicense').then((interception) => {
      expect(interception.response?.statusCode).to.eq(200);
      expect(interception.response?.body.plan).to.eq('Enterprise');
      expect(interception.response?.body.limits).to.deep.eq({
        maxActiveProcesses: null,
        maxActiveConnectors: null,
        maxAdminUsers: null,
        maxEndUsers: null,
      });
    });

    cy.contains(/Enterprise/i).should('be.visible');
  });

  it('T1-F6-05: surfaces upgrade guidance with InstanceId when Free plan limit (402) blocks 2nd active process', () => {
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
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Second Active Process');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'second-active');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createProcess').its('response.statusCode').should('eq', 402);

    byAlert().should('be.visible').and('contain.text', MOCK_IDS.instanceId);
    byLink(/Settings|License|Ustawienia|Licencj|Einstellungen|Lizenz/i).should('exist');
  });

  it('T1-F6-06: hides licensing status and activation controls from EndUser on /settings', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
      endUserLanguage: 'en',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_END_USER_CREDENTIALS.email, DEFAULT_END_USER_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    cy.get('@getLicensingStatus.all').should('have.length', 0);
    cy.contains(MOCK_IDS.instanceId).should('not.exist');
  });
});
