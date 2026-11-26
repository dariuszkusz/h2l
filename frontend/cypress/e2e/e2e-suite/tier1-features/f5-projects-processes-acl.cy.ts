import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  MOCK_IDS,
} from '../mocks/mock-api.state';
import {
  byButton,
  byDialog,
  byLabel,
  byLiveRegion,
  fillByLabel,
} from '../support/a11y-queries';
import { injectAndCheckA11y } from '../support/axe-helpers';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 1 — F5: Groups, Projects, Workflow Processes, Draft Forms & Flexible ACL (US-02.3)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T1-F5-01: creates a new group (name, code) and updates groups list', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups');

    byButton(/Create.*Group|New Group|Add Group|Utwórz grupę|Gruppe erstellen/i).first().click();
    byDialog().should('be.visible');
    fillByLabel(/Group.*Name|Nazwa grupy|Gruppenname/i, 'Software Procurement');
    fillByLabel(/Code|Kod/i, 'SOFT_PROC');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createGroup').then((interception) => {
      expect(interception.request.body).to.deep.eq({
        name: 'Software Procurement',
        code: 'SOFT_PROC',
      });
      expect(interception.response?.statusCode).to.eq(201);
      expect(interception.response?.body).to.have.property('memberCount', 0);
    });

    cy.contains('Software Procurement').should('be.visible');
    byLiveRegion().should('exist');
  });

  it('T1-F5-02: adds and removes group members via POST and DELETE /groups/{groupId}/members', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededGroup: true,
      withSeededUsers: true,
    });
    // Start with endUser not in HR group so we can add and remove cleanly
    state.users[1].groupIds = [];
    state.groups[0].memberCount = 0;
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups');

    cy.contains('Human Resources').click();
    cy.wait('@listGroupMembers');

    byLabel(/Add.*Member|Select.*User|Dodaj członka|Mitglied hinzufügen/i)
      .first()
      .select(MOCK_IDS.endUserId);
    byButton(/Add Member|Dodaj|Hinzufügen/i).first().click();

    cy.wait('@addGroupMember').then((interception) => {
      expect(interception.request.body).to.deep.eq({ userId: MOCK_IDS.endUserId });
      expect(interception.response?.statusCode).to.eq(201);
    });

    cy.contains('user@example.com').should('be.visible');
    injectAndCheckA11y({ route: ROUTES.adminUsers, theme: 'light', language: 'en' });

    byButton(/Remove|Usuń|Entfernen/i).first().click();
    cy.wait('@removeGroupMember').its('response.statusCode').should('eq', 204);
  });

  it('T1-F5-03: renders projects empty state and creates project HR', () => {
    const state = createMockState({ preset: 'initialized-free' });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects').its('response.body').should('deep.equal', []);

    byButton(/New Project|Create Project|Utwórz projekt|Projekt erstellen/i).first().click();
    fillByLabel(/Project.*Name|Nazwa|Name/i, 'HR');
    fillByLabel(/Description|Opis|Beschreibung/i, 'Onboarding & approvals');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createProject').then((interception) => {
      expect(interception.request.body).to.deep.eq({
        name: 'HR',
        description: 'Onboarding & approvals',
      });
      expect(interception.response?.statusCode).to.eq(201);
    });

    cy.contains('HR').should('be.visible');
  });

  it('T1-F5-04: creates an Active WorkflowProcess inside a project', () => {
    const state = createMockState({ preset: 'initialized-free' });
    state.projects.push({
      id: MOCK_IDS.projectHrId,
      name: 'HR',
      description: 'HR project',
      createdAt: '2026-11-25T21:00:00Z',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');

    cy.contains('HR').click();
    cy.wait('@listProcesses').its('response.body').should('deep.equal', []);

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Software Purchase');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'software-purchase');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    cy.wait('@createProcess').then((interception) => {
      expect(interception.request.body).to.deep.include({
        name: 'Software Purchase',
        externalProcessKey: 'software-purchase',
        status: 'Active',
      });
      expect(interception.response?.statusCode).to.eq(201);
    });

    cy.contains('Software Purchase').should('be.visible');
    cy.contains(/Active/i).should('be.visible');
    injectAndCheckA11y({ route: ROUTES.adminProjects, theme: 'light', language: 'en' });
  });

  it('T1-F5-05: creates multiple Draft FormDefinition steps inside a single WorkflowProcess', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededProcess: true,
    });
    state.forms = [];
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    cy.contains('Software Purchase').click();
    cy.wait('@listForms').its('response.body').should('deep.equal', []);

    byButton(/New Draft Form|New Form|Create Form|Add Form|Nowy formularz|Utwórz formularz|Neues Formular|Formular erstellen/i).first().click();
    fillByLabel(/Form.*Name|Nazwa formularza|Formularname/i, 'Request Step');
    fillByLabel(/Step.*Key|Klucz kroku|Schrittschlüssel/i, 'step-1');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();
    cy.wait('@createForm').its('response.body.status').should('eq', 'Draft');

    byButton(/New Draft Form|New Form|Create Form|Add Form|Nowy formularz|Utwórz formularz|Neues Formular|Formular erstellen/i).first().click();
    fillByLabel(/Form.*Name|Nazwa formularza|Formularname/i, 'Approval Step');
    fillByLabel(/Step.*Key|Klucz kroku|Schrittschlüssel/i, 'step-2');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();
    cy.wait('@createForm').its('response.body.status').should('eq', 'Draft');

    cy.contains('Request Step').should('be.visible');
    cy.contains('Approval Step').should('be.visible');
  });

  it('T1-F5-06: configures Flexible ACL combining WholeGroup, GroupUserSubset, and DirectUser', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededGroup: true,
      withSeededUsers: true,
      withSeededProcess: true,
    });
    const salesGroupId = '019363f0-0002-7000-8000-000000000021';
    state.groups.push({
      id: salesGroupId,
      name: 'Sales',
      code: 'SALES',
      memberCount: 1,
      createdAt: '2026-11-25T21:00:00Z',
    });
    state.users[2].groupIds = [salesGroupId];
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

    // Add WholeGroup (HR)
    byLabel(/Scope|Zakres|Bereich/i).last().select('WholeGroup');
    byLabel(/Group|Grupa|Gruppe/i).last().select(MOCK_IDS.groupHrId);
    byButton(/Add.*Rule|Dodaj regułę|Regel hinzufügen/i).first().click();

    // Add GroupUserSubset (Sales -> designer)
    byLabel(/Scope|Zakres|Bereich/i).last().select('GroupUserSubset');
    byLabel(/Group|Grupa|Gruppe/i).last().select(salesGroupId);
    byLabel(/User|Użytkownik|Benutzer/i).last().select(MOCK_IDS.designerId);
    byButton(/Add.*Rule|Dodaj regułę|Regel hinzufügen/i).first().click();

    // Add DirectUser (EndUser)
    byLabel(/Scope|Zakres|Bereich/i).last().select('DirectUser');
    byLabel(/User|Użytkownik|Benutzer/i).last().select(MOCK_IDS.endUserId);
    byButton(/Add.*Rule|Dodaj regułę|Regel hinzufügen/i).first().click();

    byButton(/Save.*ACL|Save.*Rules|Save.*Access|Save|Zapisz|Speichern/i).first().click();

    cy.wait('@updateFormAccessRules').then((interception) => {
      expect(interception.request.body.rules).to.deep.equal([
        { groupId: MOCK_IDS.groupHrId, userId: null },
        { groupId: salesGroupId, userId: MOCK_IDS.designerId },
        { groupId: null, userId: MOCK_IDS.endUserId },
      ]);
      expect(interception.response?.statusCode).to.eq(204);
    });

    byLiveRegion().should('exist');
    injectAndCheckA11y({ route: ROUTES.adminProjects, theme: 'light', language: 'en' });
  });
});
