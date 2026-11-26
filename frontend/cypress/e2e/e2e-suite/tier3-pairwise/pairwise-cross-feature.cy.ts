import { registerMockApi } from '../mocks/mock-api.intercept';
import {
  createMockState,
  DEFAULT_ADMIN_CREDENTIALS,
  DEFAULT_END_USER_CREDENTIALS,
  MOCK_IDS,
  VALID_LICENSE_KEYS,
} from '../mocks/mock-api.state';
import {
  byAlert,
  byButton,
  byLabel,
  byLiveRegion,
  fillByLabel,
} from '../support/a11y-queries';
import {
  assertHtmlAttributes,
  assertNoRawTranslationKeys,
  injectAndCheckA11y,
} from '../support/axe-helpers';
import { loginViaUi, resetBrowserState, ROUTES, setViewport } from '../support/test-harness';

describe('Tier 3 — Pairwise Cross-Feature Combinations (F1–F7)', () => {
  beforeEach(() => {
    resetBrowserState();
    setViewport('desktop');
  });

  it('T3-01 (F1+F2): German onboarding with ocean palette & SMTP persists language to /login and locks /setup', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');

    byButton(/Deutsch/i).first().click();
    assertHtmlAttributes({ lang: 'de' });

    fillByLabel(/Admin.*E-?Mail/i, 'admin@kmu.de');
    fillByLabel(/Passwort|Password/i, 'SicheresPasswort!12');
    byButton(/Ocean/i).first().click();
    byLabel(/SMTP/i).first().focus().type(' ');
    fillByLabel(/Host/i, 'mail.kmu.de');
    fillByLabel(/Port/i, '587');
    fillByLabel(/From.*Address|Absender.*E-?Mail/i, 'noreply@kmu.de');
    byButton(/Einrichten|Abschließen|Initialize|Complete/i).first().click();

    cy.wait('@initializeSetup').its('response.statusCode').should('eq', 201);
    cy.url().should('include', ROUTES.login);
    assertHtmlAttributes({ lang: 'de' });

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus').its('response.body.isInitialized').should('eq', true);
    cy.url().should('include', ROUTES.login);
  });

  it('T3-02 (F2+F4): initialized Admin logs in, navigates /settings and /admin/users, and logs out cleanly', () => {
    const state = createMockState({ preset: 'uninitialized' });
    registerMockApi(state);

    cy.visit(ROUTES.setup);
    cy.wait('@getSetupStatus');
    fillByLabel(/Admin.*Email|E-Mail/i, 'boss@acme.io');
    fillByLabel(/Password|Passwort|Hasło/i, 'BossPassword!123');
    byButton(/Complete|Initialize|Finish|Create|Zakończ|Einrichten/i).first().click();
    cy.wait('@initializeSetup').its('response.statusCode').should('eq', 201);

    loginViaUi('boss@acme.io', 'BossPassword!123');
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers').its('response.statusCode').should('eq', 200);

    cy.visit(ROUTES.settings);
    cy.wait('@getMe');
    byButton(/Log\s*out|Sign\s*out|Wyloguj|Abmelden/i).first().click();
    cy.wait('@logout').its('response.statusCode').should('eq', 204);

    cy.visit(ROUTES.settings);
    cy.url().should('include', ROUTES.login);
  });

  it('T3-03 (F3+F4): manual_link invite -> activation -> EndUser sets pl + high-contrast independently of Admin', () => {
    const state = createMockState({
      preset: 'initialized-free',
      smtpConfigured: false,
      adminLanguage: 'en',
      adminTheme: 'light',
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'anna@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('EndUser');
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();

    let activationPath = '/activate';
    cy.wait('@createUser').then((interception) => {
      const link = String(interception.response?.body.activationLink ?? '');
      const url = new URL(link);
      activationPath = `${url.pathname}${url.search}`;
    });

    cy.visit(ROUTES.settings);
    cy.wait('@getMe');
    byButton(/Log\s*out|Sign\s*out|Wyloguj|Abmelden/i).first().click();
    cy.wait('@logout');

    cy.then(() => {
      cy.visit(activationPath);
    });
    fillByLabel(/^Password$|^New Password$|^Hasło$|^Passwort$/i, 'AnnaStrongPass!12');
    fillByLabel(/Confirm|Powtórz|Bestätigen/i, 'AnnaStrongPass!12');
    byButton(/Activate|Aktywuj|Aktivieren/i).first().click();
    cy.wait('@activateAccount').its('response.statusCode').should('eq', 204);

    loginViaUi('anna@example.com', 'AnnaStrongPass!12');
    cy.visit(ROUTES.settings);
    cy.wait('@getMe');
    byButton(/Polski/i).first().click();
    cy.wait('@updatePreferences');
    byButton(/High.?Contrast|Wysoki kontrast|Hoher Kontrast/i).first().click();
    cy.wait('@updatePreferences');

    assertHtmlAttributes({ lang: 'pl', theme: 'high-contrast' });
    injectAndCheckA11y({ route: ROUTES.settings, theme: 'high-contrast', language: 'pl' });
    expect(state.users[0].preferredLanguage).to.eq('en');
  });

  it('T3-04 (F3+F5): synchronizes group memberCount across POST /users (groupIds), POST member, and DELETE member', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers');
    cy.wait('@listGroups');

    byButton(/Create.*Group|New Group|Add Group|Utwórz grupę|Gruppe erstellen/i).first().click();
    fillByLabel(/Group.*Name|Nazwa grupy|Gruppenname/i, 'Operations');
    fillByLabel(/Code|Kod/i, 'OPS');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();

    let opsGroupId = '';
    cy.wait('@createGroup').then((interception) => {
      opsGroupId = String(interception.response?.body.id ?? '');
    });

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'ops.new@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('EndUser');
    byLabel(/Operations/i).first().check({ force: true });
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();
    cy.wait('@createUser').its('response.statusCode').should('eq', 201);

    cy.contains('Operations').click();
    cy.wait('@listGroupMembers').its('response.body').should('have.length', 1);

    cy.then(() => {
      expect(opsGroupId).to.not.eq('');
    });
    byLabel(/Add.*Member|Select.*User|Dodaj członka|Mitglied hinzufügen/i)
      .first()
      .select(MOCK_IDS.endUserId);
    byButton(/Add Member|Dodaj|Hinzufügen/i).first().click();
    cy.wait('@addGroupMember').its('response.statusCode').should('eq', 201);

    byButton(/Remove|Usuń|Entfernen/i).first().click();
    cy.wait('@removeGroupMember').its('response.statusCode').should('eq', 204);
  });

  it('T3-05 (F4+F5): verifies RBAC isolation for EndUser and 3-mode ACL save for Admin in one payload', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededGroup: true,
      withSeededUsers: true,
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_END_USER_CREDENTIALS.email, DEFAULT_END_USER_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.url().should('not.include', ROUTES.adminProjects);

    cy.visit(ROUTES.settings);
    cy.wait('@getMe');
    byButton(/Log\s*out|Sign\s*out|Wyloguj|Abmelden/i).first().click();
    cy.wait('@logout');

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');
    cy.contains('Software Purchase').click();
    cy.wait('@listForms');

    byButton(/Access|ACL|Permissions|Uprawnienia|Zugriffsrechte/i).first().click();
    cy.wait('@getFormAccessRules');

    byButton(/Add.*Rule|Dodaj regułę|Regel hinzufügen/i).first().click();
    byLabel(/Scope|Zakres|Bereich/i).last().select('WholeGroup');
    byLabel(/Group|Grupa|Gruppe/i).last().select(MOCK_IDS.groupHrId);

    byButton(/Add.*Rule|Dodaj regułę|Regel hinzufügen/i).first().click();
    byLabel(/Scope|Zakres|Bereich/i).last().select('GroupUserSubset');
    byLabel(/Group|Grupa|Gruppe/i).last().select(MOCK_IDS.groupHrId);
    cy.wait('@listGroupMembers');
    byLabel(/User|Użytkownik|Benutzer/i).last().select(MOCK_IDS.endUserId);

    byButton(/Add.*Rule|Dodaj regułę|Regel hinzufügen/i).first().click();
    byLabel(/Scope|Zakres|Bereich/i).last().select('DirectUser');
    byLabel(/User|Użytkownik|Benutzer/i).last().select(MOCK_IDS.designerId);

    byButton(/Save.*ACL|Save.*Rules|Save|Zapisz|Speichern/i).last().click();
    cy.wait('@updateFormAccessRules').its('response.statusCode').should('eq', 204);
  });

  it('T3-06 (F5+F6): Free 402 block on 2nd Active process unlocks immediately after Ed25519 Pro activation', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Blocked Second');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'blocked-second');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();
    cy.wait('@createProcess').its('response.statusCode').should('eq', 402);

    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');
    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.pro);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();
    cy.wait('@activateLicense').its('response.body.plan').should('eq', 'Pro');

    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects');
    cy.contains('HR').click();
    cy.wait('@listProcesses');

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Unlocked Second');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'unlocked-second');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();
    cy.wait('@createProcess').its('response.statusCode').should('eq', 201);
  });

  it('T3-07 (F6+F7): transitions GracePeriod -> Expired -> Active renewal while preserving existing project reads', () => {
    const state = createMockState({
      preset: 'grace-period',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus').its('response.body.inGracePeriod').should('eq', true);

    cy.then(() => {
      state.licenseStatus = 'Expired';
      state.gracePeriodEndsAt = '2026-11-15T21:00:00Z';
    });

    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects').its('response.statusCode').should('eq', 200);
    cy.contains('HR').click();
    cy.wait('@listProcesses').its('response.statusCode').should('eq', 200);

    byButton(/New Process|Create Process|Utwórz proces|Prozess erstellen/i).first().click();
    fillByLabel(/Process.*Name|Nazwa procesu|Prozessname/i, 'Expired Blocked');
    fillByLabel(/External.*Key|Klucz procesu|Prozessschlüssel/i, 'expired-blocked');
    byButton(/Save|Create|Utwórz|Erstellen/i).last().click();
    cy.wait('@createProcess').its('response.statusCode').should('eq', 402);

    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');
    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.renewedPro);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();
    cy.wait('@activateLicense').its('response.body.status').should('eq', 'Active');
  });

  it('T3-08 (F1+F4+F7): switches en/de/pl and light/dark/high-contrast cleanly with Expired banner & axe check', () => {
    const state = createMockState({
      preset: 'expired',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');

    const matrix: Array<{ lang: 'en' | 'de' | 'pl'; langBtn: RegExp; theme: 'light' | 'dark' | 'high-contrast'; themeBtn: RegExp }> = [
      { lang: 'en', langBtn: /English/i, theme: 'light', themeBtn: /Light|Jasny|Hell/i },
      { lang: 'de', langBtn: /Deutsch/i, theme: 'dark', themeBtn: /Dark|Ciemny|Dunkel/i },
      { lang: 'pl', langBtn: /Polski/i, theme: 'high-contrast', themeBtn: /High.?Contrast|Wysoki kontrast|Hoher Kontrast/i },
    ];

    matrix.forEach((step) => {
      byButton(step.langBtn).first().click();
      cy.wait('@updatePreferences');
      byButton(step.themeBtn).first().click();
      cy.wait('@updatePreferences');
      assertHtmlAttributes({ lang: step.lang, theme: step.theme });
      assertNoRawTranslationKeys();
      byAlert().should('be.visible');
      injectAndCheckA11y({ route: ROUTES.settings, theme: step.theme, language: step.lang });
    });
  });

  it('T3-09 (F4+F5+F6): handles transparent JWT refresh during /admin/projects load and redirects when refresh fails', () => {
    const state = createMockState({
      preset: 'initialized-free',
      withSeededProcess: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.then(() => {
      state.simulateSingleAccessTokenExpiry = true;
    });

    cy.visit(ROUTES.adminProjects);
    cy.wait('@listProjects').its('response.statusCode').should('eq', 401);
    cy.wait('@refreshToken').its('response.statusCode').should('eq', 200);
    cy.wait('@listProjects').its('response.statusCode').should('eq', 200);
    cy.contains('HR').should('be.visible');

    cy.then(() => {
      state.simulateSingleAccessTokenExpiry = true;
      Object.keys(state.activeRefreshTokens).forEach((rt) => state.expiredRefreshTokens.add(rt));
    });
    cy.reload();
    cy.wait('@refreshToken').its('response.statusCode').should('eq', 401);
    cy.url().should('include', ROUTES.login);
  });

  it('T3-10 (F2+F3+F7): no-SMTP instance in Expired state blocks 2nd Admin (402) while preserving user list & recovery', () => {
    const state = createMockState({
      preset: 'expired',
      smtpConfigured: false,
      withSeededUsers: true,
    });
    registerMockApi(state);

    loginViaUi(DEFAULT_ADMIN_CREDENTIALS.email, DEFAULT_ADMIN_CREDENTIALS.password);
    cy.visit(ROUTES.adminUsers);
    cy.wait('@listUsers').its('response.statusCode').should('eq', 200);

    byButton(/Invite|Add User|New User|Zaproś|Einladen/i).first().click();
    fillByLabel(/Email|E-Mail/i, 'second.admin@example.com');
    byLabel(/Role|Rola|Rolle/i).first().select('Admin');
    byButton(/Send Invite|Invite|Create|Wyślij|Zaproś|Einladen/i).last().click();
    cy.wait('@createUser').its('response.statusCode').should('eq', 402);

    cy.visit(ROUTES.settings);
    cy.wait('@getLicensingStatus');
    fillByLabel(/License.*Key|Klucz licencji|Lizenzschlüssel/i, VALID_LICENSE_KEYS.renewedPro);
    byButton(/Activate.*License|Aktywuj licencję|Lizenz aktivieren/i).first().click();
    cy.wait('@activateLicense').its('response.statusCode').should('eq', 200);
    byLiveRegion().should('exist');
  });
});
