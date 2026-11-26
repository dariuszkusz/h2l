import { provideStore } from '@ngrx/store';
import { authReducer } from '../../../core/auth/store/auth.reducer';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { licensingReducer } from '../../../core/licensing/store/licensing.reducer';
import { themeReducer } from '../../../core/theme/store/theme.reducer';
import { SettingsPageComponent } from './settings-page.component';

describe('SettingsPageComponent', () => {
  it('renders preferences and passes axe checks', () => {
    cy.mount(SettingsPageComponent, {
      providers: [
        provideStore({
          i18n: i18nReducer,
          theme: themeReducer,
          auth: authReducer,
          licensing: licensingReducer,
        }),
      ],
    });

    cy.get('[data-testid="settings-heading"]').should('be.visible');
    cy.injectAxe();
    cy.checkA11y();
  });
});
