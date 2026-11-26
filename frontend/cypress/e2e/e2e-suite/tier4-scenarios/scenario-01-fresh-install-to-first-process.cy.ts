import { registerMockApi } from '../mocks/mock-api.intercept';
import { createMockState } from '../mocks/mock-api.state';
import {
  byButton,
  byHeading,
  fillByLabel,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  assertNoRawTranslationKeys,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import {
  loginViaUi,
  resetBrowserState,
  ROUTES,
  setViewport,
} from '../support/test-harness';

describe('Tier 4 — T4-01: Fresh Self-Hosted Install to First Project & Process (Polish Ty, forest + light)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T4-01: walks SysAdmin from /setup (pl, forest) -> /login -> /admin/users -> /admin/projects with axe checks', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    byButton(/Polski/i).first().click();
    assertHtmlAttributes({ lang: 'pl' });
    assertNoRawTranslationKeys();
    injectAndCheckA11y({ route: ROUTES.setup, theme: 'light', palette: 'slate', language: 'pl' });

    fillByLabel(/Admin.*E-?Mail|E-mail/i, 'sysadmin@firma.pl');
    fillByLabel(/Hasło|Password/i, 'BezpieczneHaslo!12');
    byButton(/Forest/i).first().click();
    assertHtmlAttributes({ lang: 'pl', palette: 'forest', theme: 'light' });

    byButton(/Zakończ|Utwórz|Zainicjuj|Initialize|Complete/i).first().click();
    cy.wait('@initializeSetup').its('response.statusCode').should('eq', 201);
    cy.url().should('include', ROUTES.login);

    // Confirm re-init lock
    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus').its('response.body.isInitialized').should('eq', true);
    cy.url().should('include', ROUTES.login);
    injectAndCheckA11y({ route: ROUTES.login, theme: 'light', palette: 'forest', language: 'pl' });

    loginViaUi('sysadmin@firma.pl', 'BezpieczneHaslo!12');

    // Create group HR
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups');
    byButton(/Utwórz grupę|Create.*Group|New Group/i).first().click();
    fillByLabel(/Nazwa grupy|Group.*Name/i, 'HR');
    fillByLabel(/Kod|Code/i, 'HR');
    byButton(/Zapisz|Utwórz|Save|Create/i).last().click();
    cy.wait('@createGroup').its('response.statusCode').should('eq', 201);
    injectAndCheckA11y({ route: ROUTES.adminUsers, theme: 'light', palette: 'forest', language: 'pl' });

    // Create project HR, 1st Active process, and 2 Draft forms
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    byButton(/Utwórz projekt|New Project|Create Project/i).first().click();
    fillByLabel(/Nazwa|Project.*Name/i, 'HR');
    byButton(/Zapisz|Utwórz|Save|Create/i).last().click();
    cy.wait('@createProject').its('response.statusCode').should('eq', 201);

    cy.contains('HR').click();
    cy.wait('@listProcesses');
    byButton(/Utwórz proces|New Process|Create Process/i).first().click();
    fillByLabel(/Nazwa procesu|Process.*Name/i, 'Software Purchase');
    fillByLabel(/Klucz procesu|External.*Key/i, 'software-purchase');
    byButton(/Zapisz|Utwórz|Save|Create/i).last().click();
    cy.wait('@createProcess').its('response.statusCode').should('eq', 201);

    cy.contains('Software Purchase').click();
    cy.wait('@listForms');

    ['Wniosek', 'Akceptacja'].forEach((formName, idx) => {
      byButton(/Utwórz formularz|New Form|Create Form/i).first().click();
      fillByLabel(/Nazwa formularza|Form.*Name/i, formName);
      fillByLabel(/Klucz kroku|Step.*Key/i, `step-${idx + 1}`);
      byButton(/Zapisz|Utwórz|Save|Create/i).last().click();
      cy.wait('@createForm').its('response.statusCode').should('eq', 201);
    });

    byHeading().should('be.visible');
    injectAndCheckA11y({ route: ROUTES.adminProjects, theme: 'light', palette: 'forest', language: 'pl' });
  });
});
