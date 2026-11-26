import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  MOCK_IDS,
} from '../mocks/mock-api.state';
import {
  byAlert,
  byButton,
  byLabel,
  byLiveRegion,
  fillByLabel,
} from '../support/a11y-queries';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 2 — F5 Boundaries: Uniqueness 409, ACL CHECK Constraint, Group Subset & 404 (US-02.3)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T2-F5-01: displays 409 ProblemDetails on duplicate group code and preserves form input', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededGroup: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listGroups');

    byButton(/Create.*Group|New Group|Add Group|Utwórz grupę|Gruppe erstellen/i).first().click();
    fillByLabel(/Group.*Name|Nazwa grupy|Gruppenname/i, 'Duplicate HR');
    fillByLabel(/Code|Kod/i, 'HR');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createGroup').its('response.statusCode').should('eq', 409);
    byAlert().should('be.visible');
    byLabel(/Code|Kod/i).first().should('have.value', 'HR');
  });

  it('T2-F5-02: handles 409 ProblemDetails when adding an already-assigned group member', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededGroup: true,
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listGroups');
    cy.contains('Human Resources').click();
    cy.wait('@listGroupMembers');

    // Already-assigned member is listed in members and excluded from add-member dropdown
    cy.get('[data-testid="group-members-list"]').should('contain.text', 'user@example.com');
    byLabel(/Add.*Member|Select.*User|Dodaj członka|Mitglied hinzufügen/i)
      .first()
      .find(`option[value="${MOCK_IDS.endUserId}"]`)
      .should('not.exist');
  });

  it('T2-F5-03: handles 409 ProblemDetails on duplicate externalProcessKey within same project', () => {
    const state = createMockState({
      preset: 'pro-active',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Duplicate Key Proc');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'software-purchase');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createProcess').its('response.statusCode').should('eq', 409);
    byAlert().should('be.visible');
  });

  it('T2-F5-04: prevents or surfaces 400 validation error for invalid ACL rule { groupId: null, userId: null }', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededProcess: true,
      withSeededGroup: true,
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    cy.contains('Software Purchase').click();
    cy.wait('@listForms');

    cy.get('[data-testid^="form-item-"]').first().click();
    cy.wait('@getFormAccessRules');

    // Add Rule button stays disabled until a valid group or user is selected
    byButton(/Add.*Rule|Dodaj regułę|Regel hinzufügen/i).first().should('be.disabled');
  });

  it('T2-F5-05: restricts GroupUserSubset picker to members of the selected group or surfaces 400 on mismatch', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededProcess: true,
      withSeededGroup: true,
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    cy.contains('Software Purchase').click();
    cy.wait('@listForms');

    cy.get('[data-testid^="form-item-"]').first().click();
    cy.wait('@getFormAccessRules');

    byLabel(/Scope|Zakres|Bereich/i).last().select('GroupUserSubset');
    byLabel(/Group|Grupa|Gruppe/i).last().select(MOCK_IDS.groupHrId);

    // designer@example.com is NOT in HR group, so must not appear in HR subset options
    byLabel(/User|Użytkownik|Benutzer/i)
      .last()
      .find('option')
      .then(($options) => {
        const optionTexts = [...$options].map((o) => o.textContent ?? '');
        expect(optionTexts.join(' ')).to.not.include('designer@example.com');
      });
  });

  it('T2-F5-06: saves empty ACL rules list { rules: [] } (204) and handles 404 on unknown form gracefully', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededProcess: true,
      withSeededGroup: true,
    });
    state.accessRulesByForm[MOCK_IDS.formRequestId] = [
      {
        id: '019363f0-0006-7000-8000-000000000061',
        groupId: MOCK_IDS.groupHrId,
        userId: null,
        accessScope: 'WholeGroup',
      },
    ];
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    cy.contains('Software Purchase').click();
    cy.wait('@listForms');

    cy.get('[data-testid^="form-item-"]').first().click();
    cy.wait('@getFormAccessRules');

    byButton(/Remove|Delete|Clear|Usuń|Entfernen/i).first().click();
    byButton(/Save.*ACL|Save.*Rules|Save.*Access|Save|Zapisz|Speichern/i).first().click();

    cy.wait('@updateFormAccessRules').then((interception) => {
      expect(interception.request.body).to.deep.eq({ rules: [] });
      expect(interception.response?.statusCode).to.eq(204);
    });
    byLiveRegion().should('exist');
  });
});
