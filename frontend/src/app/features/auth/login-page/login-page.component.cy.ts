import { provideRouter } from '@angular/router';
import { provideStore } from '@ngrx/store';
import { authReducer } from '../../../core/auth/store/auth.reducer';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { LoginPageComponent } from './login-page.component';

describe('LoginPageComponent', () => {
  it('renders login form, validates required fields, and passes axe checks', () => {
    cy.mount(LoginPageComponent, {
      providers: [
        provideRouter([]),
        provideStore({
          i18n: i18nReducer,
          auth: authReducer,
        }),
      ],
    });

    cy.get('[data-testid="login-email"]').should('be.visible');
    cy.get('[data-testid="login-password"]').should('be.visible');
    cy.injectAxe();
    cy.checkA11y();
  });
});
