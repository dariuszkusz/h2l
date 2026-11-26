import { provideRouter } from '@angular/router';
import { provideStore } from '@ngrx/store';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { adminUsersReducer } from '../store/admin-users.reducer';
import { AdminUsersPageComponent } from './admin-users-page.component';

describe('AdminUsersPageComponent', () => {
  it('renders users and groups admin panel, opens invite modal, and passes axe checks', () => {
    cy.mount(AdminUsersPageComponent, {
      providers: [
        provideRouter([]),
        provideStore({
          i18n: i18nReducer,
          adminUsers: adminUsersReducer,
        }),
      ],
    });

    cy.get('[data-testid="admin-users-heading"]').should('be.visible');
    cy.injectAxe();
    cy.checkA11y();

    cy.get('[data-testid="open-invite-user-btn"]').click();
    cy.get('[data-testid="invite-user-dialog"]').should('be.visible');
    cy.checkA11y();
  });
});
