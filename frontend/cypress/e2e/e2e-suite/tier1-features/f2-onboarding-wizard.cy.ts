import { registerMockApi } from '../mocks/mock-api.intercept';
import { createMockState } from '../mocks/mock-api.state';
import {
  byButton,
  byHeading,
  byLabel,
  byStatus,
  fillByLabel,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import {
  activateFocusedWithKeyboard,
  assertElementKeyboardFocusable,
  resetBrowserState,
  ROUTES,
  setViewport,
} from '../support/test-harness';

describe('Tier 1 — F2: Instance Onboarding Wizard & Re-Init Lock (US-01.2)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T1-F2-01: completes onboarding without SMTP using slate palette and en', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    byButton(/English/i).first().click();
    fillByLabel(/Admin.*Email|E-Mail/i, 'founder@acme.io');
    fillByLabel(/Password|Passwort|Hasło/i, 'FounderPass!1234');
    byButton(/Slate/i).first().click();

    assertHtmlAttributes({ lang: 'en', palette: 'slate' });
    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();

    cy.wait('@initializeSetup').then((interception) => {
      expect(interception.request.body).to.deep.include({
        adminEmail: 'founder@acme.io',
        adminPassword: 'FounderPass!1234',
        defaultLanguage: 'en',
        selectedPalette: 'slate',
      });
      expect(interception.request.body.smtp ?? null).to.eq(null);
      expect(interception.response?.statusCode).to.eq(201);
      expect(interception.response?.body).to.have.property('instanceId');
      expect(interception.response?.body).to.have.property('adminUserId');
    });

    cy.url().should('include', ROUTES.login);
  });

  it('T1-F2-02: completes onboarding with full SMTP configuration and ocean palette', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    byButton(/Deutsch/i).first().click();
    fillByLabel(/Admin.*E-?Mail/i, 'ops@acme.de');
    fillByLabel(/Passwort|Password/i, 'GermanAdminPass!12');
    byButton(/Ocean|Ozean/i).first().click();

    byLabel(/SMTP/i).first().focus().type(' ');
    fillByLabel(/Host/i, 'smtp.acme.de');
    fillByLabel(/Port/i, '587');
    fillByLabel(/From.*Address|Absender.*E-?Mail/i, 'noreply@acme.de');
    fillByLabel(/Username|Benutzername/i, 'mailer@acme.de');
    fillByLabel(/Passwort \(optional\)|Password \(optional\)|SMTP.*Password|SMTP.*Passwort/i, 'SmtpSecret!99');

    byLabel(/Passwort \(optional\)|Password \(optional\)|SMTP.*Password|SMTP.*Passwort/i)
      .first()
      .should('have.attr', 'type', 'password');
    injectAndCheckA11y({ route: ROUTES.setup, theme: 'light', palette: 'ocean', language: 'de' });

    byButton(/Einrichten|Abschließen|Initialize|Complete/i).first().click();

    cy.wait('@initializeSetup').then((interception) => {
      expect(interception.request.body).to.deep.include({
        adminEmail: 'ops@acme.de',
        defaultLanguage: 'de',
        selectedPalette: 'ocean',
      });
      expect(interception.request.body.smtp).to.deep.include({
        host: 'smtp.acme.de',
        port: 587,
        fromAddress: 'noreply@acme.de',
      });
      expect(interception.response?.statusCode).to.eq(201);
    });
  });

  it('T1-F2-03: previews only the 3 predefined palettes (slate, ocean, forest) across Light, Dark, and High Contrast', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    cy.get('input[type="color"]').should('not.exist');

    const palettes: Array<'slate' | 'ocean' | 'forest'> = ['slate', 'ocean', 'forest'];
    const themes: Array<'light' | 'dark' | 'high-contrast'> = ['light', 'dark', 'high-contrast'];

    palettes.forEach((palette) => {
      byButton(new RegExp(palette, 'i')).first().click();
      assertHtmlAttributes({ palette });
      themes.forEach((theme) => {
        byButton(new RegExp(theme === 'high-contrast' ? 'high.?contrast' : theme, 'i'))
          .first()
          .click();
        assertHtmlAttributes({ theme, palette });
        injectAndCheckA11y({ route: ROUTES.setup, theme, palette, language: 'en' });
      });
    });
  });

  it('T1-F2-04: redirects away from /setup to /login when instance is already initialized', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus').its('response.body.isInitialized').should('eq', true);

    cy.url().should('include', ROUTES.login);
    cy.get('@initializeSetup.all').should('have.length', 0);
  });

  it('T1-F2-05: shows accessible loading skeleton while GET /setup/status is pending', () => {
    const state = createMockState({ preset: 'uninitialized' });
    state.setupStatusDelayMs = 600;
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    byStatus().should('exist');
    cy.wait('@getSetupStatus');
    byHeading().should('be.visible');
  });

  it('T1-F2-06: completes onboarding with forest palette and Polish language via keyboard only', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    assertElementKeyboardFocusable(byButton(/Polski/i));
    activateFocusedWithKeyboard('Enter');
    assertHtmlAttributes({ lang: 'pl' });

    assertElementKeyboardFocusable(byButton(/Forest|Leśn|Wald/i));
    activateFocusedWithKeyboard('Enter');
    assertHtmlAttributes({ palette: 'forest' });

    fillByLabel(/Admin.*E-?Mail|E-mail/i, 'admin@firma.pl');
    fillByLabel(/Hasło|Password/i, 'PolskieHaslo!123');

    assertElementKeyboardFocusable(byButton(/Zakończ|Utwórz|Zainicjuj|Initialize|Complete/i));
    activateFocusedWithKeyboard('Enter');

    cy.wait('@initializeSetup').then((interception) => {
      expect(interception.request.body).to.deep.include({
        defaultLanguage: 'pl',
        selectedPalette: 'forest',
      });
      expect(interception.response?.statusCode).to.eq(201);
    });

    cy.url().should('include', ROUTES.login);
  });
});
