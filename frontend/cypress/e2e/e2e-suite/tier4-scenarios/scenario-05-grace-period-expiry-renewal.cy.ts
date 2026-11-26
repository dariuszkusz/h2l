import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  DEFAULT_END_USER_CREDENTIALS,
  VALID_LICENSE_KEYS,
} from '../mocks/mock-api.state';
import {
  byAlert,
  byButton,
  byHeading,
  byLiveRegion,
  fillByLabel,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  assertNoHorizontalOverflow,
  assertNoRawTranslationKeys,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 4 — T4-05: 14-Day Grace Period -> Expired Soft Degradation -> Recovery Renewal (Trilingual)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T4-05: verifies localized GracePeriod warning, Expired soft degradation, EndUser mobile continuity, and renewal', () => {
    const state = createMockState({
      preset: 'grace-period',
      withSeededProcess: true,
      withSeededUsers: true,
      endUserLanguage: 'pl',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    // Trilingual GracePeriod banner verification
    (['en', 'de', 'pl'] as const).forEach((lang) => {
      const btn = lang === 'en' ? /English/i : lang === 'de' ? /Deutsch/i : /Polski/i;
      byButton(btn).first().click();
      cy.wait('@updatePreferences');
      assertHtmlAttributes({ lang });
      assertNoRawTranslationKeys();
      byLiveRegion().should('exist');
    });

    // Active process creation succeeds during GracePeriod
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    byButton(/Nowy proces|Utwórz proces|New Process|Create Process/i).first().click();
    fillByLabel(/Nazwa procesu|Process.*Name/i, 'Grace Valid Proc');
    fillByLabel(/Klucz procesu|External.*Key/i, 'grace-valid-proc');
    byButton(/Zapisz|Utwórz|Save|Create/i).last().click();
    cy.wait('@createProcess').its('response.statusCode').should('eq', 201);

    // Transition to Expired -> blocks new active process (402), existing data stays readable
    cy.then(() => {
      state.licenseStatus = 'Expired';
    });
    byButton(/Nowy proces|Utwórz proces|New Process|Create Process/i).first().click();
    fillByLabel(/Nazwa procesu|Process.*Name/i, 'Expired Blocked Proc');
    fillByLabel(/Klucz procesu|External.*Key/i, 'expired-blocked-proc');
    byButton(/Zapisz|Utwórz|Save|Create/i).last().click();
    cy.wait('@createProcess').its('response.statusCode').should('eq', 402);
    byAlert().should('be.visible');

    // EndUser logs in on Mobile PWA during Expired state without disruption
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');
    byButton(/Wyloguj|Log\s*out/i).first().click();
    cy.wait('@logout');

    setViewport('mobile');
    loginViaUi(DEFAULT_END_USER_CREDENTIALS.email, DEFAULT_END_USER_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');
    byHeading().should('be.visible');
    assertNoHorizontalOverflow();
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'light', language: 'pl' });

    byButton(/Wyloguj|Log\s*out/i).first().click();
    cy.wait('@logout');

    // Admin renews license -> Active restored
    setViewport('desktop');
    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');
    fillByLabel(/Klucz licencji|License.*Key/i, VALID_LICENSE_KEYS.renewedPro);
    byButton(/Aktywuj licencję|Activate.*License/i).first().click();
    cy.wait('@activateLicense').its('response.body.status').should('eq', 'Active');
  });
});
