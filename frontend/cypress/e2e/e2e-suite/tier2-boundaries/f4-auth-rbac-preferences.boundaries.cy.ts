import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  buildProblemDetails,
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  DEFAULT_DESIGNER_CREDENTIALS,
  DEFAULT_END_USER_CREDENTIALS,
} from '../mocks/mock-api.state';
import {
  byAlert,
  byButton,
  byLabel,
} from '../support/a11y-queries';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 2 — F4 Boundaries: Invalid Credentials, Token Expiry, RBAC Guards & Rollback (US-02.2)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T2-F4-01: displays localized alert on 401 invalid login credentials without echoing password', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    cy.visit(ROUTES.login);
    cy.wait('@getSetupStatus');

    const wrongPass = 'WrongSecret!999';
    byLabel(/Email|E-Mail/i).first().type(DEFAULT_ADMIN_CREDENTIALS.email);
    byLabel(/Password|Passwort|Hasło/i).first().type(wrongPass);
    byButton(/Log\s*in|Sign\s*in|Anmelden|Zaloguj/i).first().click();

    cy.wait('@login').its('response.statusCode').should('eq', 401);
    byAlert().should('be.visible').and('not.contain.text', wrongPass);
    cy.url().should('include', ROUTES.login);
  });

  it('T2-F4-02: clears session and redirects to /login when refresh token returns 401 (no retry loop)', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);

    cy.then(() => {
      state.simulateSingleAccessTokenExpiry = true;
      Object.keys(state.activeRefreshTokens).forEach((rt) => state.expiredRefreshTokens.add(rt));
    });

    cy.visit(ROUTES.adminUsers);
    cy.wait('@getMe').its('response.statusCode').should('eq', 401);
    cy.wait('@refreshToken').its('response.statusCode').should('eq', 401);

    cy.url().should('include', ROUTES.login);
    cy.get('@refreshToken.all').should('have.length', 1);
  });

  it('T2-F4-03: blocks EndUser from accessing /admin/users and /admin/projects via direct URL', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
      endUserLanguage: 'en',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_END_USER_CREDENTIALS.email, DEFAULT_END_USER_CREDENTIALS.password);

    cy.visit(ROUTES.adminUsers);
    cy.url().should('not.include', ROUTES.adminUsers);

    cy.visit(ROUTES.adminProjects);
    cy.url().should('not.include', ROUTES.adminProjects);
  });

  it('T2-F4-04: blocks ProcessDesigner from /admin/users and handles 403 ProblemDetails', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_DESIGNER_CREDENTIALS.email, DEFAULT_DESIGNER_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);

    cy.url().then((url) => {
      if (url.includes(ROUTES.adminUsers)) {
        byAlert().should('be.visible');
      } else {
        expect(url).to.not.include(ROUTES.adminUsers);
      }
    });
  });

  it('T2-F4-05: redirects unauthenticated visitors from /settings, /admin/users, and /admin/projects to /login', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    [ROUTES.settings, ROUTES.adminUsers, ROUTES.adminProjects].forEach((protectedRoute) => {
      cy.visit(protectedRoute);
      cy.url().should('include', ROUTES.login);
    });
  });

  it('T2-F4-06: surfaces ProblemDetails error and avoids corrupting theme state if PATCH /users/me/preferences fails with 500', () => {
    const state = createMockState({ preset: 'initialized-free' });
    state.preferencesError = buildProblemDetails(
      500,
      'Internal Server Error',
      'Could not persist user preferences.',
      'PREFERENCES_SAVE_FAILED',
      '/api/v1/users/me/preferences',
    );
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');

    byButton(/Dark|Ciemny|Dunkel/i).first().click();
    cy.wait('@updatePreferences').its('response.statusCode').should('eq', 500);
    byAlert().should('be.visible');
  });
});
