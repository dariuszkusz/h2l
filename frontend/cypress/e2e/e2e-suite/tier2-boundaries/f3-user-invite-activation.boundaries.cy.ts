import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
} from '../mocks/mock-api.state';
import {
  assertInvalidField,
  byAlert,
  byButton,
  byLabel,
  fillByLabel,
} from '../support/a11y-queries';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 2 — F3 Boundaries: Duplicate Email, Token Expiry & Password Length (US-02.1)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T2-F3-01: displays 409 ProblemDetails alert when inviting an already-existing email', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'user@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('EndUser');
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    cy.wait('@createUser').its('response.statusCode').should('eq', 409);
    byAlert().should('be.visible');
  });

  it('T2-F3-02: exposes manual_link only on creation and never leaks activation tokens in GET /users', () => {
    const state = createMockState({
      preset: 'initialized-free',
      smtpConfigured: false,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'airgap@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('EndUser');
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    cy.wait('@createUser').then((interception) => {
      expect(interception.response?.body.deliveryMethod).to.eq('manual_link');
      expect(interception.response?.body.activationLink).to.include('/activate?token=');
    });

    cy.reload();
    cy.wait('@listUsers').then((interception) => {
      const serialized = JSON.stringify(interception.response?.body);
      expect(serialized).to.not.include('activationLink');
      expect(serialized).to.not.include('act_');
    });
  });

  it('T2-F3-03: enforces activation password length boundaries (11 rejected, 129 rejected, 12 accepted)', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
    });
    registerMockApi(state);

    cy.visit(`${ROUTES.activate}?token=one-time-token-123`);

    fillByLabel(/^Password$|^New Password$|^Hasło$|^Passwort$/i, 'ElevenChar!');
    fillByLabel(/Confirm|Powtórz|Bestätigen/i, 'ElevenChar!');
    byButton(/Activate|Aktywuj|Aktivieren/i).first().click();
    assertInvalidField(/^Password$|^New Password$|^Hasło$|^Passwort$/i);

    fillByLabel(/^Password$|^New Password$|^Hasło$|^Passwort$/i, 'X'.repeat(129));
    fillByLabel(/Confirm|Powtórz|Bestätigen/i, 'X'.repeat(129));
    byButton(/Activate|Aktywuj|Aktivieren/i).first().click();
    assertInvalidField(/^Password$|^New Password$|^Hasło$|^Passwort$/i);
    cy.get('@activateAccount.all').should('have.length', 0);

    const exact12 = 'TwelveChar!1';
    fillByLabel(/^Password$|^New Password$|^Hasło$|^Passwort$/i, exact12);
    fillByLabel(/Confirm|Powtórz|Bestätigen/i, exact12);
    byButton(/Activate|Aktywuj|Aktivieren/i).first().click();
    cy.wait('@activateAccount').its('response.statusCode').should('eq', 204);
  });

  it('T2-F3-04: shows 409 ProblemDetails alert when activating with an already-used token', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
    });
    registerMockApi(state);

    cy.visit(`${ROUTES.activate}?token=used-tok`);
    fillByLabel(/^Password$|^New Password$|^Hasło$|^Passwort$/i, 'ValidPass!123');
    fillByLabel(/Confirm|Powtórz|Bestätigen/i, 'ValidPass!123');
    byButton(/Activate|Aktywuj|Aktivieren/i).first().click();

    cy.wait('@activateAccount').its('response.statusCode').should('eq', 409);
    byAlert().should('be.visible');
  });

  it('T2-F3-05: handles missing or empty ?token query parameter without sending empty token request', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    cy.visit(ROUTES.activate);
    byAlert().should('be.visible');
    cy.get('@activateAccount.all').should('have.length', 0);
  });

  it('T2-F3-06: rejects invalid email format and externalId > 200 chars in invite dialog', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'bad@');
    fillByLabel(/External.*ID/i, 'E'.repeat(201));
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    assertInvalidField(/Email|E-Mail/i);
    assertInvalidField(/External.*ID/i);
    cy.get('@createUser.all').should('have.length', 0);
  });
});
