import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  DEFAULT_END_USER_CREDENTIALS,
} from '../mocks/mock-api.state';
import {
  byButton,
  byLabel,
  byLink,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import {
  assertBearerTokenPresent,
  loginViaUi,
  resetBrowserState,
  ROUTES,
  setViewport,
} from '../support/test-harness';

describe('Tier 1 — F4: JWT Authentication, RBAC & Independent User Preferences (US-02.2)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T1-F4-01: Admin logs in with valid credentials, attaches Bearer token, and sees Admin navigation', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    cy.visit(ROUTES.login);
    cy.wait('@getSetupStatus');

    byLabel(/Email|E-Mail/i).first().type(DEFAULT_ADMIN_CREDENTIALS.email);
    byLabel(/Password|Passwort|Hasło/i).first().type(DEFAULT_ADMIN_CREDENTIALS.password);
    byButton(/Log\s*in|Sign\s*in|Anmelden|Zaloguj/i).first().click();

    cy.wait('@login').then((interception) => {
      expect(interception.request.body).to.deep.eq({
        email: DEFAULT_ADMIN_CREDENTIALS.email,
        password: DEFAULT_ADMIN_CREDENTIALS.password,
      });
      expect(interception.response?.statusCode).to.eq(200);
      expect(interception.response?.body).to.have.property('accessToken');
      expect(interception.response?.body).to.have.property('refreshToken');
      expect(interception.response?.body.user).to.have.property('systemRole', 'Admin');
    });

    cy.url().should('not.include', ROUTES.login);
    byLink(/Users|Użytkownicy|Benutzer/i).should('exist');
    byLink(/Projects|Projekty|Projekte/i).should('exist');
    injectAndCheckA11y({ route: 'authenticated-admin', theme: 'light', language: 'en' });
  });

  it('T1-F4-02: EndUser logs in and RBAC hides Admin navigation links', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
      endUserLanguage: 'en',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_END_USER_CREDENTIALS.email, DEFAULT_END_USER_CREDENTIALS.password);

    cy.get('a[href*="/admin/users"]').should('not.exist');
    cy.get('a[href*="/admin/projects"]').should('not.exist');
    byLink(/Settings|Ustawienia|Einstellungen/i).should('exist');
  });

  it('T1-F4-03: logs out via POST /auth/logout with Bearer token and blocks protected routes afterward', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    byButton(/Log\s*out|Sign\s*out|Wyloguj|Abmelden/i).first().click();

    cy.wait('@logout').then((interception) => {
      assertBearerTokenPresent(interception.request.headers);
      expect(interception.request.body).to.have.property('refreshToken').and.be.a('string');
      expect(interception.response?.statusCode).to.eq(204);
    });

    cy.url().should('include', ROUTES.login);
    cy.visit(ROUTES.settings);
    cy.url().should('include', ROUTES.login);
  });

  it('T1-F4-04: updates preferredTheme independently across light, dark, and high-contrast', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    byButton(/Dark|Ciemny|Dunkel/i).first().click();
    cy.wait('@updatePreferences').its('request.body').should('deep.include', { preferredTheme: 'dark' });
    assertHtmlAttributes({ theme: 'dark' });
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'dark', language: 'en' });

    byButton(/High.?Contrast|Wysoki kontrast|Hoher Kontrast/i).first().click();
    cy.wait('@updatePreferences').its('request.body').should('deep.include', { preferredTheme: 'high-contrast' });
    assertHtmlAttributes({ theme: 'high-contrast' });
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'high-contrast', language: 'en' });
  });

  it('T1-F4-05: updates preferredLanguage independently (en -> de -> pl) and restores on reload', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    byButton(/Deutsch/i).first().click();
    cy.wait('@updatePreferences').its('request.body').should('deep.include', { preferredLanguage: 'de' });
    assertHtmlAttributes({ lang: 'de' });

    cy.reload();
    cy.wait('@getMe').its('response.body.preferredLanguage').should('eq', 'de');
    assertHtmlAttributes({ lang: 'de' });
  });

  it('T1-F4-06: transparently refreshes expired access token via POST /auth/refresh and retries GET /users/me', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.then(() => {
      state.simulateSingleAccessTokenExpiry = true;
    });

    cy.visit(ROUTES.settings);
    cy.wait('@getMe').its('response.statusCode').should('eq', 401);
    cy.wait('@refreshToken').its('response.statusCode').should('eq', 200);
    cy.wait('@getMe').its('response.statusCode').should('eq', 200);

    cy.url().should('include', ROUTES.settings);
  });
});
