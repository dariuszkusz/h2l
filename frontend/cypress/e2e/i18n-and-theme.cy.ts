import type { Result } from 'axe-core';

function logAxeViolations(violations: Result[]): void {
  const details = violations
    .map((violation) => {
      const targets = violation.nodes
        .map((node) => `${node.target.join(', ')} (${node.failureSummary ?? ''})`)
        .join(' | ');
      return `[${violation.impact}] ${violation.id}: ${violation.help} -> ${targets}`;
    })
    .join('\n');
  throw new Error(`Accessibility violations detected:\n${details}`);
}

const MOCK_INSTANCE_ID = '01900000-0000-7000-8000-000000000001';

const MOCK_USER_EN = {
  id: '01900000-0000-7000-8000-000000000010',
  email: 'user@example.com',
  externalId: null,
  systemRole: 'EndUser',
  preferredLanguage: 'en',
  preferredTheme: 'light',
  isActive: true,
  createdAt: '2026-11-25T20:00:00Z',
};

describe('h2l First-Run i18n, Runtime Switching, Theme Modes & WCAG 2.1 AA', () => {
  beforeEach(() => {
    cy.clearLocalStorage();
    cy.window().then((win) => {
      win.sessionStorage.clear();
    });
    cy.intercept('GET', '/api/v1/setup/status', {
      statusCode: 200,
      body: {
        isInitialized: true,
        instanceId: MOCK_INSTANCE_ID,
      },
    }).as('getSetupStatus');
  });

  it('displays first-run language picker, previews de/pl, persists choice across reload, and passes axe', () => {
    cy.intercept('POST', '/api/v1/auth/login', {
      statusCode: 200,
      body: {
        accessToken: 'test-access-token',
        refreshToken: 'test-refresh-token',
        expiresInSeconds: 900,
        user: {
          ...MOCK_USER_EN,
          preferredLanguage: 'pl',
        },
      },
    }).as('loginRequest');

    cy.visit('/');
    cy.wait('@getSetupStatus');

    cy.get('[data-testid="first-run-language-dialog"]').should('be.visible');
    cy.injectAxe();
    cy.checkA11y(undefined, undefined, logAxeViolations);

    cy.get('[data-testid="first-run-lang-de"]').click();
    cy.get('[data-testid="first-run-language-dialog"]').should('contain.text', 'Wähle deine Sprache');

    cy.get('[data-testid="first-run-lang-pl"]').click();
    cy.get('[data-testid="first-run-language-dialog"]').should('contain.text', 'Wybierz swój język');

    cy.get('[data-testid="first-run-lang-confirm"]').click();
    cy.get('[data-testid="first-run-language-dialog"]').should('not.exist');
    cy.get('html').should('have.attr', 'lang', 'pl');

    cy.reload();
    cy.wait('@getSetupStatus');
    cy.get('[data-testid="first-run-language-dialog"]').should('not.exist');
    cy.get('html').should('have.attr', 'lang', 'pl');

    cy.get('[data-testid="login-email"]').type('user@example.com');
    cy.get('[data-testid="login-password"]').type('CorrectHorseBatteryStaple!');
    cy.get('[data-testid="login-submit"]').click();
    cy.wait('@loginRequest');

    cy.contains('Twoje zadania workflow').should('be.visible');
  });

  it('switches en/de/pl at runtime in Settings and keeps profile languages independent', () => {
    cy.intercept('GET', '/api/v1/users/me', {
      statusCode: 200,
      body: MOCK_USER_EN,
    }).as('getMe');

    cy.visit('/', {
      onBeforeLoad(win) {
        win.sessionStorage.setItem('h2l.auth.accessToken', 'test-access-token');
        win.sessionStorage.setItem('h2l.auth.refreshToken', 'test-refresh-token');
      },
    });
    cy.wait(['@getSetupStatus', '@getMe']);

    cy.get('[data-testid="nav-settings"]').click();
    cy.get('[data-testid="settings-heading"]').should('contain.text', 'Preferences');

    cy.get('[data-testid="settings-lang-de"]').click();
    cy.get('[data-testid="settings-heading"]').should('contain.text', 'Einstellungen');
    cy.get('html').should('have.attr', 'lang', 'de');

    cy.get('[data-testid="settings-lang-pl"]').click();
    cy.get('[data-testid="settings-heading"]').should('contain.text', 'Ustawienia');
    cy.get('html').should('have.attr', 'lang', 'pl');

    // Switch to admin profile — has no saved preference yet, so first-run dialog appears independently
    cy.get('[data-testid="session-profile-admin"]').click();
    cy.get('[data-testid="first-run-language-dialog"]').should('be.visible');
    cy.get('[data-testid="first-run-lang-en"]').click();
    cy.get('[data-testid="first-run-lang-confirm"]').click();
    cy.get('html').should('have.attr', 'lang', 'en');

    // Switching back to default profile restores Polish independently
    cy.get('[data-testid="session-profile-default"]').click();
    cy.get('html').should('have.attr', 'lang', 'pl');
  });

  it('switches Light, Dark, and High Contrast modes with predefined palettes and passes axe WCAG 2.1 AA', () => {
    cy.intercept('GET', '/api/v1/users/me', {
      statusCode: 200,
      body: MOCK_USER_EN,
    }).as('getMe');

    cy.visit('/', {
      onBeforeLoad(win) {
        win.sessionStorage.setItem('h2l.auth.accessToken', 'test-access-token');
        win.sessionStorage.setItem('h2l.auth.refreshToken', 'test-refresh-token');
      },
    });
    cy.wait(['@getSetupStatus', '@getMe']);

    cy.get('[data-testid="nav-settings"]').click();
    cy.injectAxe();

    cy.get('[data-testid="theme-mode-light"]').click();
    cy.get('html').should('have.attr', 'data-theme', 'light');
    cy.checkA11y(undefined, undefined, logAxeViolations);

    cy.get('[data-testid="theme-mode-dark"]').click();
    cy.get('[data-testid="palette-ocean"]').click();
    cy.get('html').should('have.attr', 'data-theme', 'dark');
    cy.get('html').should('have.attr', 'data-palette', 'ocean');
    cy.get('html').should('have.class', 'dark');
    cy.checkA11y(undefined, undefined, logAxeViolations);

    cy.get('[data-testid="theme-mode-high-contrast"]').click();
    cy.get('[data-testid="palette-forest"]').click();
    cy.get('html').should('have.attr', 'data-theme', 'high-contrast');
    cy.get('html').should('have.attr', 'data-palette', 'forest');
    cy.checkA11y(undefined, undefined, logAxeViolations);
  });
});
