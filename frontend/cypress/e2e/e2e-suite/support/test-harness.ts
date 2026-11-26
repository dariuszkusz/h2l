import type { AuthTokenResponse, UserProfileResponse } from '../mocks/mock-api.types';

export const ROUTES = {
  setup: '/setup',
  login: '/login',
  activate: '/activate',
  adminUsers: '/admin/users',
  adminProjects: '/admin/projects',
  settings: '/settings',
  tasks: '/tasks',
} as const;

export const VIEWPORTS = {
  desktop: { width: 1280, height: 800 },
  mobile: { width: 390, height: 844 },
} as const;

export function setViewport(preset: keyof typeof VIEWPORTS): void {
  const { width, height } = VIEWPORTS[preset];
  cy.viewport(width, height);
}

export function clearIndexedDb(): Cypress.Chainable<void> {
  return cy.window({ log: false }).then(async (win) => {
    if (typeof win.indexedDB?.databases === 'function') {
      const dbs = await win.indexedDB.databases();
      await Promise.all(
        dbs
          .map((db) => db.name)
          .filter((name): name is string => typeof name === 'string' && name.length > 0)
          .map(
            (name) =>
              new Promise<void>((resolve) => {
                const req = win.indexedDB.deleteDatabase(name);
                req.onsuccess = () => resolve();
                req.onerror = () => resolve();
                req.onblocked = () => resolve();
              }),
          ),
      );
    }
  });
}

export function resetBrowserState(): void {
  cy.clearAllLocalStorage();
  cy.clearAllSessionStorage();
  cy.clearAllCookies();
  clearIndexedDb();
}

export function visitWithReloadGuard(path: string): void {
  cy.visit(path);
  cy.window().then((win) => {
    (win as unknown as Record<string, unknown>)['__h2lSpaMarker'] = 'no-full-reload';
  });
}

export function assertNoFullPageReload(): void {
  cy.window().then((win) => {
    expect((win as unknown as Record<string, unknown>)['__h2lSpaMarker']).to.eq('no-full-reload');
  });
}

export function assertElementKeyboardFocusable(
  chain: Cypress.Chainable<JQuery<HTMLElement>>,
): Cypress.Chainable<JQuery<HTMLElement>> {
  return chain.first().focus().should('be.focused');
}

export function activateFocusedWithKeyboard(key: 'Enter' | 'Space' = 'Enter'): void {
  const sequence = key === 'Enter' ? '{enter}' : ' ';
  cy.focused().type(sequence);
}

export function pressEscapeKey(): void {
  cy.focused().type('{esc}');
}

export function loginViaUi(email: string, password: string): void {
  cy.visit(ROUTES.login);
  cy.wait('@getSetupStatus');
  cy.get('input[type="email"], input[name="email"]').first().clear().type(email);
  cy.get('input[type="password"], input[name="password"]').first().clear().type(password);
  cy.get('button[type="submit"]').first().click();
  cy.wait('@login');
}

export function assertBearerTokenPresent(
  headers: Record<string, string | string[] | undefined>,
  expectedToken?: string,
): void {
  const authHeader = headers['authorization'] ?? headers['Authorization'];
  const headerValue = Array.isArray(authHeader) ? authHeader[0] : authHeader;
  expect(headerValue, 'Authorization Bearer header must be present').to.be.a('string').and.match(/^Bearer\s+\S+/);
  if (expectedToken !== undefined) {
    expect(headerValue).to.eq(`Bearer ${expectedToken}`);
  }
}

export type SessionBundle = AuthTokenResponse & {
  user: UserProfileResponse;
};
