import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
} from '../mocks/mock-api.state';
import {
  byButton,
  byLabel,
  byLiveRegion,
  fillByLabel,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 4 — T4-03: Enterprise Department ACL Workflow (WholeGroup + Subset + DirectUser, ocean + high-contrast)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T4-03: configures WholeGroup(HR) on Step 1 and GroupUserSubset(Sales, salesSoft) + DirectUser(cfo) on Step 2', () => {
    const state = createMockState({
      preset: 'enterprise-active',
      adminTheme: 'high-contrast',
    });
    state.selectedPalette = 'ocean';
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups');

    let hrGroupId = '';
    let salesGroupId = '';
    let salesSoftId = '';
    let cfoId = '';

    byButton(/Create.*Group|New Group/i).first().click();
    fillByLabel(/Group.*Name/i, 'HR');
    fillByLabel(/Code/i, 'HR');
    byButton(/Save|Create/i).last().click();
    cy.wait('@createGroup').then((i) => {
      hrGroupId = String(i.response?.body.id ?? '');
    });

    byButton(/Create.*Group|New Group/i).first().click();
    fillByLabel(/Group.*Name/i, 'Sales');
    fillByLabel(/Code/i, 'SALES');
    byButton(/Save|Create/i).last().click();
    cy.wait('@createGroup').then((i) => {
      salesGroupId = String(i.response?.body.id ?? '');
    });

    // Invite hr1 -> HR, salesSoft -> Sales, salesHardware -> Sales, cfo -> direct
    byButton(/Invite|Add User/i).first().click();
    fillByLabel(/Email/i, 'hr1@acme.com');
    byLabel(/Role/i).first().select('EndUser');
    byLabel(/HR/i).first().check({ force: true });
    byButton(/Send Invite|Invite|Create/i).last().click();
    cy.wait('@createUser');

    byButton(/Invite|Add User/i).first().click();
    fillByLabel(/Email/i, 'sales.soft@acme.com');
    byLabel(/Role/i).first().select('EndUser');
    byLabel(/Sales/i).first().check({ force: true });
    byButton(/Send Invite|Invite|Create/i).last().click();
    cy.wait('@createUser').then((i) => {
      salesSoftId = String(i.response?.body.id ?? '');
    });

    byButton(/Invite|Add User/i).first().click();
    fillByLabel(/Email/i, 'sales.hardware@acme.com');
    byLabel(/Role/i).first().select('EndUser');
    byLabel(/Sales/i).first().check({ force: true });
    byButton(/Send Invite|Invite|Create/i).last().click();
    cy.wait('@createUser');

    byButton(/Invite|Add User/i).first().click();
    fillByLabel(/Email/i, 'cfo@acme.com');
    byLabel(/Role/i).first().select('EndUser');
    byButton(/Send Invite|Invite|Create/i).last().click();
    cy.wait('@createUser').then((i) => {
      cfoId = String(i.response?.body.id ?? '');
    });

    // Create Procurement -> Software Purchase -> Request + Approval
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    byButton(/New Project|Create Project/i).first().click();
    fillByLabel(/Project.*Name/i, 'Procurement');
    byButton(/Save|Create/i).last().click();
    cy.wait('@createProject');

    cy.contains('Procurement').click();
    cy.wait('@listProcesses');
    byButton(/New Process|Create Process/i).first().click();
    fillByLabel(/Process.*Name/i, 'Software Purchase');
    fillByLabel(/External.*Key/i, 'software-purchase');
    byButton(/Save|Create/i).last().click();
    cy.wait('@createProcess');

    cy.contains('Software Purchase').click();
    cy.wait('@listForms');

    byButton(/New Form|Create Form/i).first().click();
    fillByLabel(/Form.*Name/i, 'Request');
    fillByLabel(/Step.*Key/i, 'request');
    byButton(/Save|Create/i).last().click();
    cy.wait('@createForm');

    byButton(/New Form|Create Form/i).first().click();
    fillByLabel(/Form.*Name/i, 'Approval');
    fillByLabel(/Step.*Key/i, 'approval');
    byButton(/Save|Create/i).last().click();
    cy.wait('@createForm');

    // Step 1 ACL: WholeGroup(HR)
    byButton(/Access|ACL|Permissions/i).first().click();
    cy.wait('@getFormAccessRules');
    byButton(/Add.*Rule/i).first().click();
    byLabel(/Scope/i).last().select('WholeGroup');
    cy.then(() => {
      byLabel(/Group/i).last().select(hrGroupId);
    });
    byButton(/Save.*ACL|Save.*Rules|Save/i).last().click();
    cy.wait('@updateFormAccessRules').its('response.statusCode').should('eq', 204);

    // Step 2 ACL: GroupUserSubset(Sales, salesSoft) + DirectUser(cfo)
    byButton(/Access|ACL|Permissions/i).last().click();
    cy.wait('@getFormAccessRules');
    byButton(/Add.*Rule/i).first().click();
    byLabel(/Scope/i).last().select('GroupUserSubset');
    cy.then(() => {
      byLabel(/Group/i).last().select(salesGroupId);
    });
    cy.wait('@listGroupMembers');
    cy.then(() => {
      byLabel(/User/i).last().select(salesSoftId);
    });

    byButton(/Add.*Rule/i).first().click();
    byLabel(/Scope/i).last().select('DirectUser');
    cy.then(() => {
      byLabel(/User/i).last().select(cfoId);
    });

    byButton(/Save.*ACL|Save.*Rules|Save/i).last().click();
    cy.wait('@updateFormAccessRules').then((interception) => {
      expect(interception.request.body.rules).to.deep.equal([
        { groupId: salesGroupId, userId: salesSoftId },
        { groupId: null, userId: cfoId },
      ]);
      expect(interception.response?.statusCode).to.eq(204);
    });

    byLiveRegion().should('exist');
    assertHtmlAttributes({ theme: 'high-contrast' });
    injectAndCheckA11y({ route: ROUTES.adminProjects, theme: 'high-contrast', palette: 'ocean', language: 'en' });
  });
});
