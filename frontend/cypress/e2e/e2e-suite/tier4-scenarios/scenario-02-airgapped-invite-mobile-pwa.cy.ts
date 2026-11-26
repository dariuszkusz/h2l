import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
} from '../mocks/mock-api.state';
import {
  byButton,
  byLabel,
  byLiveRegion,
  fillByLabel,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  assertMobileTouchTargets,
  assertNoHorizontalOverflow,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import {
  loginViaUi,
  resetBrowserState,
  ROUTES,
  setViewport,
} from '../support/test-harness';

describe('Tier 4 — T4-02: Air-Gapped Invite (manual_link), Activation & Mobile PWA Accessibility (German du, dark)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T4-02: invites anna@example.com without SMTP, activates on mobile 390x844 in German + dark mode with 44px touch targets', () => {
    const state = createMockState({
      preset: 'initialized-free',
      smtpConfigured: false,
      adminLanguage: 'en',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');

    byButton(/Invite|Add User|New User/i).first().click();
    fillByLabel(/Email/i, 'anna@example.com');
    byLabel(/Role/i).first().select('EndUser');
    byButton(/Send Invite|Invite|Create/i).last().click();

    let activationPath = '/activate';
    cy.wait('@createUser').then((interception) => {
      expect(interception.response?.body.deliveryMethod).to.eq('manual_link');
      const link = String(interception.response?.body.activationLink ?? '');
      const url = new URL(link);
      activationPath = `${url.pathname}${url.search}`;
    });

    byButton(/Copy/i).first().click();
    byLiveRegion().should('exist');

    cy.visit(ROUTES.settings);
    cy.wait('@getMe');
    byButton(/Log\s*out|Sign\s*out/i).first().click();
    cy.wait('@logout');

    // Switch to Mobile PWA viewport for EndUser
    setViewport('mobile');
    cy.then(() => {
      cy.visit(activationPath);
    });

    byButton(/Deutsch/i).first().click();
    assertHtmlAttributes({ lang: 'de' });
    assertNoHorizontalOverflow();
    assertMobileTouchTargets(44);

    fillByLabel(/^Passwort$|^Neues Passwort$|^Password$/i, 'AnnaMobilePass!14');
    fillByLabel(/Bestätigen|Wiederholen|Confirm/i, 'AnnaMobilePass!14');
    byButton(/Aktivieren|Activate/i).first().click();
    cy.wait('@activateAccount').its('response.statusCode').should('eq', 204);

    loginViaUi('anna@example.com', 'AnnaMobilePass!14');
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    byButton(/Deutsch/i).first().click();
    cy.wait('@updatePreferences').its('request.body').should('deep.include', { preferredLanguage: 'de' });
    byButton(/Dunkel|Dark/i).first().click();
    cy.wait('@updatePreferences').its('request.body').should('deep.include', { preferredTheme: 'dark' });

    assertHtmlAttributes({ lang: 'de', theme: 'dark' });
    assertNoHorizontalOverflow();
    assertMobileTouchTargets(44);
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'dark', language: 'de' });
  });
});
