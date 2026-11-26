import { provideRouter } from '@angular/router';
import { provideStore } from '@ngrx/store';
import { authReducer } from '../../../core/auth/store/auth.reducer';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { ActivatePageComponent } from './activate-page.component';

describe('ActivatePageComponent', () => {
  it('renders account activation form and passes axe checks', () => {
    cy.mount(ActivatePageComponent, {
      providers: [
        provideRouter([]),
        provideStore({
          i18n: i18nReducer,
          auth: authReducer,
        }),
      ],
    });

    cy.get('[data-testid="activate-heading"]').should('be.visible');
    cy.injectAxe();
    cy.checkA11y();
  });
});
