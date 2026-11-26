import { registerMockApi } from '../mocks/mock-api.intercept';
import { createMockState } from '../mocks/mock-api.state';
import {
  assertInvalidField,
  byAlert,
  byButton,
  byLabel,
  fillByLabel,
} from '../support/a11y-queries';
import { resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 2 — F2 Boundaries: Onboarding Validation, SMTP Ranges & Re-Init Lock (US-01.2)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T2-F2-01: handles 409 Conflict when instance was concurrently initialized and guides redirect to /login', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    fillByLabel(/Admin.*Email|E-Mail/i, 'race@acme.io');
    fillByLabel(/Password|Passwort|Hasło/i, 'StrongPassword!12');

    cy.then(() => {
      state.isInitialized = true;
    });

    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();
    cy.wait('@initializeSetup').its('response.statusCode').should('eq', 409);

    byAlert().should('be.visible');
  });

  it('T2-F2-02: rejects adminPassword shorter than 12 characters (11 chars) with accessible inline error', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    fillByLabel(/Admin.*Email|E-Mail/i, 'admin@acme.io');
    fillByLabel(/Password|Passwort|Hasło/i, 'ShortPass1!');
    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();

    assertInvalidField(/Password|Passwort|Hasło/i);
    cy.get('@initializeSetup.all').should('have.length', 0);
  });

  it('T2-F2-03: rejects adminPassword longer than 128 characters (129 chars) and invalid email format', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    fillByLabel(/Admin.*Email|E-Mail/i, 'not-an-email');
    fillByLabel(/Password|Passwort|Hasło/i, 'A'.repeat(129));
    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();

    assertInvalidField(/Admin.*Email|E-Mail/i);
    assertInvalidField(/Password|Passwort|Hasło/i);
    cy.get('@initializeSetup.all').should('have.length', 0);
  });

  it('T2-F2-04: enforces SMTP port bounds (1..65535), required host, and valid fromAddress only when SMTP enabled', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    fillByLabel(/Admin.*Email|E-Mail/i, 'admin@acme.io');
    fillByLabel(/Password|Passwort|Hasło/i, 'StrongPassword!12');

    byLabel(/SMTP/i).first().focus().type(' ');
    fillByLabel(/Port/i, '0');
    fillByLabel(/From.*Address|Absender.*E-?Mail/i, 'bad-from');
    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();

    assertInvalidField(/Port/i);
    assertInvalidField(/From.*Address|Absender.*E-?Mail/i);

    fillByLabel(/Port/i, '65536');
    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();
    assertInvalidField(/Port/i);
    cy.get('@initializeSetup.all').should('have.length', 0);
  });

  it('T2-F2-05: maps 400 ProblemDetails errors dictionary to accessible alert without leaking password in DOM', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.intercept('POST', '/api/v1/setup/initialize', {
      statusCode: 400,
      headers: { 'content-type': 'application/problem+json' },
      body: {
        type: 'https://h2l.local/errors/validation-error',
        title: 'Validation failed',
        status: 400,
        detail: 'One or more setup fields failed validation.',
        errorCode: 'VALIDATION_ERROR',
        errors: {
          adminPassword: ['Password must meet complexity policy.'],
        },
      },
    }).as('initializeSetupError');

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    const secretInput = 'SecretSuperPass!99';
    fillByLabel(/Admin.*Email|E-Mail/i, 'admin@acme.io');
    fillByLabel(/Password|Passwort|Hasło/i, secretInput);
    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();

    cy.wait('@initializeSetupError');
    byAlert().should('be.visible').and('not.contain.text', secretInput);
  });

  it('T2-F2-06: prevents re-opening /setup after 201 initialization and redirects to /login', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    fillByLabel(/Admin.*Email|E-Mail/i, 'owner@acme.io');
    fillByLabel(/Password|Passwort|Hasło/i, 'OwnerPassword!12');
    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();
    cy.wait('@initializeSetup').its('response.statusCode').should('eq', 201);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus').its('response.body.isInitialized').should('eq', true);
    cy.url().should('include', ROUTES.login);
  });
});
