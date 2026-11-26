import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  buildProblemDetails,
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  DEFAULT_END_USER_CREDENTIALS,
} from '../mocks/mock-api.state';
import {
  byAlert,
  byButton,
  byHeading,
  byLabel,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  assertNoHorizontalOverflow,
  assertNoRawTranslationKeys,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import {
  loginViaUi,
  resetBrowserState,
  ROUTES,
  setViewport,
} from '../support/test-harness';

describe('Tier 2 — F1 Boundaries: i18n Overflow, Zoom, Isolation, Fallback & Tone (US-01.1)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T2-F1-01: renders long German and Polish compound labels without horizontal overflow on desktop and mobile', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    (['de', 'pl'] as const).forEach((lang) => {
      const btn = lang === 'de' ? /Deutsch/i : /Polski/i;
      byButton(btn).first().click();
      assertHtmlAttributes({ lang });

      setViewport('desktop');
      assertNoHorizontalOverflow();

      setViewport('mobile');
      assertNoHorizontalOverflow();
      assertNoRawTranslationKeys();
    });
  });

  it('T2-F1-02: maintains 200% text-scale readability in de and pl without overlap or horizontal scroll', () => {
    const state = createMockState({ preset: 'initialized-free', adminLanguage: 'de' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    cy.document().then((doc) => {
      doc.documentElement.style.fontSize = '32px';
    });

    assertNoHorizontalOverflow();
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'de' });

    byButton(/Polski/i).first().click();
    cy.wait('@updatePreferences');
    assertNoHorizontalOverflow();
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'pl' });
  });

  it('T2-F1-03: keeps Admin language (en) isolated from EndUser preferredLanguage (pl)', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
      adminLanguage: 'en',
      endUserLanguage: 'pl',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe').its('response.body.preferredLanguage').should('eq', 'en');
    assertHtmlAttributes({ lang: 'en' });

    byButton(/Log\s*out|Sign\s*out|Wyloguj|Abmelden/i).first().click();
    cy.wait('@logout');

    loginViaUi(DEFAULT_END_USER_CREDENTIALS.email, DEFAULT_END_USER_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe').its('response.body.preferredLanguage').should('eq', 'pl');
    assertHtmlAttributes({ lang: 'pl' });
  });

  it('T2-F1-04: renders accessible error state on GET /setup/status 503 and recovers on retry', () => {
    const state = createMockState({ preset: 'uninitialized' });
    state.setupStatusError = buildProblemDetails(
      503,
      'Service Unavailable',
      'Database readiness probe failed.',
      'SERVICE_UNAVAILABLE',
      '/api/v1/setup/status',
    );
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus').its('response.statusCode').should('eq', 503);
    byAlert().should('be.visible');

    cy.then(() => {
      state.setupStatusError = null;
    });

    byButton(/Retry|Try again|Ponów|Erneut/i).first().click();
    cy.wait('@getSetupStatus').its('response.statusCode').should('eq', 200);
    byHeading().should('be.visible');
  });

  it('T2-F1-05: enforces informal direct address tone (du in de, Ty in pl) without formal pronouns', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    byButton(/Deutsch/i).first().click();
    cy.get('main, [role="main"], body').invoke('text').then((deText) => {
      expect(deText).to.not.match(/\b(Sie|Ihre|Ihnen)\b/);
    });

    byButton(/Polski/i).first().click();
    cy.get('main, [role="main"], body').invoke('text').then((plText) => {
      expect(plText).to.not.match(/\b(Pan|Pani|Państwa)\b/);
    });
  });

  it('T2-F1-06: falls back gracefully when localStorage contains an unknown locale code', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    cy.visit(ROUTES.login, {
      onBeforeLoad(win) {
        win.localStorage.setItem('h2l_lang', 'xx-UNKNOWN');
        win.localStorage.setItem('lang', 'xx-UNKNOWN');
      },
    });
    cy.wait('@getSetupStatus');

    cy.get('html')
      .invoke('attr', 'lang')
      .should('be.oneOf', ['en', 'de', 'pl']);
    assertNoRawTranslationKeys();
    byLabel(/Email|E-Mail/i).first().should('be.visible');
  });
});
