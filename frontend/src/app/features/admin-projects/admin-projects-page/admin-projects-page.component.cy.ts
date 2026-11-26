import { provideRouter } from '@angular/router';
import { provideStore } from '@ngrx/store';
import { authReducer } from '../../../core/auth/store/auth.reducer';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { adminUsersReducer } from '../../admin-users/store/admin-users.reducer';
import { adminProjectsReducer } from '../store/admin-projects.reducer';
import { AdminProjectsPageComponent } from './admin-projects-page.component';

describe('AdminProjectsPageComponent', () => {
  it('renders projects, processes, and ACL panel and passes axe checks', () => {
    cy.mount(AdminProjectsPageComponent, {
      providers: [
        provideRouter([]),
        provideStore({
          i18n: i18nReducer,
          auth: authReducer,
          adminUsers: adminUsersReducer,
          adminProjects: adminProjectsReducer,
        }),
      ],
    });

    cy.get('[data-testid="admin-projects-heading"]').should('be.visible');
    cy.injectAxe();
    cy.checkA11y();
  });
});
