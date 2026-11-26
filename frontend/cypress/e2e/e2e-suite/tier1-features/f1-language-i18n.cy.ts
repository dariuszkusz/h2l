import { registerMockApi } from '../mocks/mock-api.intercept';
import { createMockState, DEFAULT_ADMIN_CREDENTIALS } from '../mocks/mock-api.state';
import {
  byButton,
  byHeading,
  byLabel,
  byLiveRegion,
  byRole,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  assertNoRawTranslationKeys,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import {
  activateFocusedWithKeyboard,
  assertElementKeyboardFocusable,
  assertNoFullPageReload,
  loginViaUi,
  resetBrowserState,
  ROUTES,
  setViewport,
  visitWithReloadGuard,
} from '../support/test-harness';

describe('Tier 1 — F1: First-Run Language Picker & i18n Switching (US-01.1)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T1-F1-01: renders keyboard-accessible first-run language picker when instance is uninitialized', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus').then((interception) => {
      expect(interception.response?.statusCode).to.eq(200);
      expect(interception.response?.body).to.have.property('isInitialized', false);
      expect(interception.response?.body).to.have.property('instanceId').and.match(/^[0-9a-f-]{36}$/i);
    });

    assertElementKeyboardFocusable(byButton(/English/i));
    assertElementKeyboardFocusable(byButton(/Deutsch/i));
    assertElementKeyboardFocusable(byButton(/Polski/i));

    injectAndCheckA11y({ route: ROUTES.setup, theme: 'light', language: 'en' });
  });

  it('T1-F1-02: switches interface immediately to German (de) without page reload', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    visitWithReloadGuard(ROUTES.setup);
    cy.wait('@getSetupStatus');

    byButton(/Deutsch/i).first().click();

    assertNoFullPageReload();
    assertHtmlAttributes({ lang: 'de' });
    byHeading().first().should('not.be.empty');
    assertNoRawTranslationKeys();
  });

  it('T1-F1-03: switches interface immediately to Polish (pl) using keyboard only', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    visitWithReloadGuard(ROUTES.setup);
    cy.wait('@getSetupStatus');

    assertElementKeyboardFocusable(byButton(/Polski/i));
    activateFocusedWithKeyboard('Enter');

    assertNoFullPageReload();
    assertHtmlAttributes({ lang: 'pl' });
    assertNoRawTranslationKeys();
    byRole('button').first().should('be.visible');
  });

  it('T1-F1-04: switches language on /login before auth and persists across transition to /activate', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    visitWithReloadGuard(ROUTES.login);
    cy.wait('@getSetupStatus').its('response.body.isInitialized').should('eq', true);

    byButton(/Deutsch/i).first().click();
    assertHtmlAttributes({ lang: 'de' });
    byLabel(/E-Mail|Email/i).first().should('be.visible');
    byLabel(/Passwort|Password/i).first().should('be.visible');

    visitWithReloadGuard(`${ROUTES.activate}?token=one-time-token-123`);
    assertHtmlAttributes({ lang: 'de' });
    assertNoRawTranslationKeys();
  });

  it('T1-F1-05: hydrates authenticated user preferredLanguage on /settings and switches live via PATCH', () => {
    const state = createMockState({
      preset: 'initialized-free',
      adminLanguage: 'pl',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe').its('response.body.preferredLanguage').should('eq', 'pl');
    assertHtmlAttributes({ lang: 'pl' });

    byButton(/English/i).first().click();

    cy.wait('@updatePreferences').then((interception) => {
      expect(interception.request.body).to.deep.include({ preferredLanguage: 'en' });
      expect(interception.response?.statusCode).to.eq(204);
    });

    assertHtmlAttributes({ lang: 'en' });
    byLiveRegion().should('exist');
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'en' });
  });

  it('T1-F1-06: keeps language switcher accessible across light, dark, and high-contrast themes', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    const themes: Array<'light' | 'dark' | 'high-contrast'> = ['light', 'dark', 'high-contrast'];
    const langs: Array<{ button: RegExp; code: 'en' | 'de' | 'pl' }> = [
      { button: /English/i, code: 'en' },
      { button: /Deutsch/i, code: 'de' },
      { button: /Polski/i, code: 'pl' },
    ];

    themes.forEach((theme, index) => {
      const targetLang = langs[index % langs.length];
      byButton(new RegExp(theme === 'high-contrast' ? 'high.?contrast|wysoki kontrast|hoher kontrast' : theme, 'i'))
        .first()
        .click();
      cy.wait('@updatePreferences');
      byButton(targetLang.button).first().click();
      cy.wait('@updatePreferences');
      assertHtmlAttributes({ lang: targetLang.code, theme });
      injectAndCheckA11y({ route: ROUTES.settings, theme, language: targetLang.code });
    });
  });
});
