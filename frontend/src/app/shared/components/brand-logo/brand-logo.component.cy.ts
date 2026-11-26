import { provideStore } from '@ngrx/store';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { BrandLogoComponent } from './brand-logo.component';

describe('BrandLogoComponent', () => {
  it('renders full logo (mark + wordmark) by default and passes axe checks', () => {
    cy.mount(BrandLogoComponent, {
      componentProperties: {
        variant: 'full',
        size: 'md',
        showTagline: true,
      },
      providers: [provideStore({ i18n: i18nReducer })],
    });

    cy.get('[data-testid="brand-logo-mark"]').should('be.visible').and('have.attr', 'aria-hidden', 'true');
    cy.get('[data-testid="brand-logo-wordmark"]').should('be.visible').and('contain.text', 'h2l');
    cy.get('[data-testid="brand-logo-tagline"]').should('be.visible');

    cy.injectAxe();
    cy.checkA11y();
  });

  it('renders mark-only variant with accessible image role and label', () => {
    cy.mount(BrandLogoComponent, {
      componentProperties: {
        variant: 'mark',
        size: 'lg',
      },
      providers: [provideStore({ i18n: i18nReducer })],
    });

    cy.get('[data-testid="brand-logo-mark"]')
      .should('be.visible')
      .and('have.attr', 'role', 'img')
      .and('have.attr', 'aria-label', 'h2l');
    cy.get('[data-testid="brand-logo-wordmark"]').should('not.exist');

    cy.injectAxe();
    cy.checkA11y();
  });

  it('renders wordmark-only variant without the icon mark', () => {
    cy.mount(BrandLogoComponent, {
      componentProperties: {
        variant: 'wordmark',
        size: 'sm',
        showTagline: false,
      },
      providers: [provideStore({ i18n: i18nReducer })],
    });

    cy.get('[data-testid="brand-logo-mark"]').should('not.exist');
    cy.get('[data-testid="brand-logo-wordmark"]').should('be.visible').and('contain.text', 'h2l');
    cy.get('[data-testid="brand-logo-tagline"]').should('not.exist');

    cy.injectAxe();
    cy.checkA11y();
  });
});
