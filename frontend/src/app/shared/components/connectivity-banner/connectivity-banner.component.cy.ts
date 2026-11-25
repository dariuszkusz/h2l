import { provideStore } from '@ngrx/store';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { ConnectivityBannerComponent } from './connectivity-banner.component';

describe('ConnectivityBannerComponent', () => {
  it('renders online status and passes axe checks', () => {
    cy.mount(ConnectivityBannerComponent, {
      componentProperties: {
        isOnline: true,
        pendingDraftsCount: 0,
        syncStatus: 'idle',
      },
      providers: [provideStore({ i18n: i18nReducer })],
    });

    cy.get('[data-testid="connectivity-banner"]').should('contain.text', 'Online');
    cy.injectAxe();
    cy.checkA11y();
  });

  it('renders offline queued drafts status with text and icon', () => {
    cy.mount(ConnectivityBannerComponent, {
      componentProperties: {
        isOnline: false,
        pendingDraftsCount: 2,
        syncStatus: 'queued',
      },
      providers: [provideStore({ i18n: i18nReducer })],
    });

    cy.get('[data-testid="connectivity-banner"]')
      .should('contain.text', 'Offline')
      .and('contain.text', 'Pending drafts: 2');
  });
});
