import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
} from '../mocks/mock-api.state';
import {
  byButton,
  byDialog,
  byHeading,
  byLabel,
  byRole,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import {
  activateFocusedWithKeyboard,
  assertElementKeyboardFocusable,
  loginViaUi,
  pressEscapeKey,
  resetBrowserState,
  ROUTES,
  setViewport,
} from '../support/test-harness';

describe('Tier 4 — T4-06: Keyboard-Only & Screen-Reader Semantic Audit Across All 6 Stage 1 Routes', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T4-06: audits landmarks, headings, keyboard focus, dialog Escape close, and cypress-axe across all 6 routes', () => {
    // 1. /setup (uninitialized)
    const setupState = createMockState({ preset: 'uninitialized' });
    registerMockApi(setupState);
    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');
    byRole('main').should('exist');
    byHeading().first().should('be.visible');
    assertElementKeyboardFocusable(byLabel(/Admin.*Email|E-Mail/i));
    injectAndCheckA11y({ route: ROUTES.setup, theme: 'light', language: 'en' });

    // 2. /activate
    const activeState = createMockState({
      preset: 'initialized-free',
      withSeededGroup: true,
      withSeededUsers: true,
      withSeededProcess: true,
    });
    registerMockApi(activeState);
    cy.visit(`${ROUTES.activate}?token=one-time-token-123`);
    byRole('main').should('exist');
    assertElementKeyboardFocusable(byLabel(/^Password$|^New Password$/i));
    injectAndCheckA11y({ route: ROUTES.activate, theme: 'light', language: 'en' });

    // 3. /login
    cy.visit(ROUTES.login);
    cy.wait('@getSetupStatus');
    byRole('main').should('exist');
    assertElementKeyboardFocusable(byLabel(/Email/i));
    injectAndCheckA11y({ route: ROUTES.login, theme: 'light', language: 'en' });

    // Authenticate as Admin
    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);

    // 4. /admin/users (keyboard modal open + Escape close)
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups');
    byRole('navigation').should('exist');
    byRole('main').should('exist');
    assertElementKeyboardFocusable(byButton(/Invite|Add User/i));
    activateFocusedWithKeyboard('Enter');
    byDialog().should('be.visible');
    pressEscapeKey();
    byDialog().should('not.exist');
    injectAndCheckA11y({ route: ROUTES.adminUsers, theme: 'light', language: 'en' });

    // 5. /admin/projects
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    byRole('main').should('exist');
    injectAndCheckA11y({ route: ROUTES.adminProjects, theme: 'light', language: 'en' });

    // 6. /settings across Dark and High-Contrast
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');
    cy.wait('@getLicensingStatus');

    assertElementKeyboardFocusable(byButton(/Dark/i));
    activateFocusedWithKeyboard('Enter');
    cy.wait('@updatePreferences');
    assertHtmlAttributes({ theme: 'dark' });
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'dark', language: 'en' });

    assertElementKeyboardFocusable(byButton(/High.?Contrast/i));
    activateFocusedWithKeyboard('Enter');
    cy.wait('@updatePreferences');
    assertHtmlAttributes({ theme: 'high-contrast' });
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'high-contrast', language: 'en' });
  });
});
