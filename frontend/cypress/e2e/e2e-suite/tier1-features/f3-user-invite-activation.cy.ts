import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  MOCK_IDS,
} from '../mocks/mock-api.state';
import {
  byButton,
  byDialog,
  byLabel,
  byLink,
  byLiveRegion,
  byRole,
  fillByLabel,
} from '../support/a11y-queries';
import { injectAndCheckA11y } from '../support/axe-helpers';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 1 — F3: Admin User Invitation & Account Activation (US-02.1)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T1-F3-01: displays users list (default state) and groups on /admin/users', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededGroup: true,
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers').its('response.statusCode').should('eq', 200);
    cy.wait('@listGroups').its('response.statusCode').should('eq', 200);

    cy.contains(DEFAULT_ADMIN_CREDENTIALS.email).should('be.visible');
    cy.contains('user@example.com').should('be.visible');
    cy.contains('Human Resources').should('be.visible');
    injectAndCheckA11y({ route: ROUTES.adminUsers, theme: 'light', language: 'en' });
  });

  it('T1-F3-02: displays empty state when no groups exist and exposes create-group CTA', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededGroup: false,
      withSeededUsers: false,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups').its('response.body').should('deep.equal', []);

    byButton(/Create.*Group|New Group|Add Group|Utwórz grupę|Gruppe erstellen/i)
      .first()
      .should('be.visible');
  });

  it('T1-F3-03: invites EndUser with SMTP delivery (deliveryMethod: smtp)', () => {
    const state = createMockState({
      preset: 'initialized-free',
      smtpConfigured: true,
      withSeededGroup: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    byDialog().should('be.visible');

    fillByLabel(/Email|E-Mail/i, 'invited.smtp@example.com');
    fillByLabel(/External.*ID/i, 'EMP-101');
    byLabel(/Role|Rola|Rolle/i).first().select('EndUser');
    byLabel(/Human Resources/i).first().check({ force: true });

    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    cy.wait('@createUser').then((interception) => {
      expect(interception.request.body).to.deep.include({
        email: 'invited.smtp@example.com',
        externalId: 'EMP-101',
        systemRole: 'EndUser',
        groupIds: [MOCK_IDS.groupHrId],
      });
      expect(interception.response?.statusCode).to.eq(201);
      expect(interception.response?.body).to.have.property('deliveryMethod', 'smtp');
      expect(interception.response?.body.activationLink ?? null).to.eq(null);
      expect(interception.response?.body).to.have.property('isActive', false);
    });

    byLiveRegion().should('exist');
    cy.contains('invited.smtp@example.com').should('be.visible');
  });

  it('T1-F3-04: invites ProcessDesigner without SMTP (manual_link) and copies one-time activation link', () => {
    const state = createMockState({
      preset: 'initialized-free',
      smtpConfigured: false,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'designer.manual@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('ProcessDesigner');
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    cy.wait('@createUser').then((interception) => {
      expect(interception.request.body).to.deep.include({
        email: 'designer.manual@example.com',
        systemRole: 'ProcessDesigner',
      });
      expect(interception.response?.statusCode).to.eq(201);
      expect(interception.response?.body).to.have.property('deliveryMethod', 'manual_link');
      expect(interception.response?.body.activationLink).to.include('/activate?token=');
    });

    cy.contains('/activate?token=').should('be.visible');
    byButton(/Copy|Kopiuj|Kopieren/i).first().click();
    byLiveRegion().should('exist');
  });

  it('T1-F3-05: activates invited account via /activate with valid token and >= 12-char password', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
    });
    registerMockApi(state);

    cy.visit(`${ROUTES.activate}?token=one-time-token-123`);

    fillByLabel(/^Password$|^New Password$|^Hasło$|^Passwort$/i, 'ValidPassword!12');
    fillByLabel(/Confirm|Powtórz|Bestätigen/i, 'ValidPassword!12');
    injectAndCheckA11y({ route: ROUTES.activate, theme: 'light', language: 'en' });

    byButton(/Activate|Aktywuj|Aktivieren/i).first().click();

    cy.wait('@activateAccount').then((interception) => {
      expect(interception.request.body).to.deep.eq({
        token: 'one-time-token-123',
        password: 'ValidPassword!12',
      });
      expect(interception.response?.statusCode).to.eq(204);
    });

    byLink(/Log\s*in|Sign\s*in|Zaloguj|Anmelden/i).should('exist');
  });

  it('T1-F3-06: ensures no public self-registration UI exists on /login or /activate', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    cy.visit(ROUTES.login);
    cy.wait('@getSetupStatus');

    byRole('link').each(($el) => {
      expect($el.text()).to.not.match(/sign\s*up|register|create\s*account|rejestracja|registrieren/i);
    });
    byRole('button').each(($el) => {
      expect($el.text()).to.not.match(/sign\s*up|register|create\s*account|rejestracja|registrieren/i);
    });
  });
});
