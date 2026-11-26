import { provideRouter } from '@angular/router';
import { provideStore } from '@ngrx/store';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { setupReducer } from '../../../core/setup/store/setup.reducer';
import { themeReducer } from '../../../core/theme/store/theme.reducer';
import { SetupPageComponent } from './setup-page.component';

describe('SetupPageComponent', () => {
  it('renders setup wizard form with palette picker and passes axe checks', () => {
    cy.mount(SetupPageComponent, {
      providers: [
        provideRouter([]),
        provideStore({
          i18n: i18nReducer,
          theme: themeReducer,
          setup: setupReducer,
        }),
      ],
    });

    cy.get('[data-testid="setup-heading"]').should('be.visible');
    cy.injectAxe();
    cy.checkA11y();
  });
});
