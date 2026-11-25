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

describe('h2l First-Run i18n, Runtime Switching, Theme Modes & WCAG 2.1 AA', () => {
  beforeEach(() => {
    cy.clearLocalStorage();
    cy.visit('/');
  });

  it('displays first-run language picker, previews de/pl, persists choice, and passes axe', () => {
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
    cy.contains('Twoje zadania workflow').should('be.visible');

    cy.reload();
    cy.get('[data-testid="first-run-language-dialog"]').should('not.exist');
    cy.get('html').should('have.attr', 'lang', 'pl');
  });

  it('switches en/de/pl at runtime in Settings and keeps profile languages independent', () => {
    cy.get('[data-testid="first-run-lang-en"]').click();
    cy.get('[data-testid="first-run-lang-confirm"]').click();

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
    cy.get('[data-testid="first-run-lang-en"]').click();
    cy.get('[data-testid="first-run-lang-confirm"]').click();
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
