import { UiStateBannerComponent } from './ui-state-banner.component';

describe('UiStateBannerComponent', () => {
  it('renders error banner with field errors and passes axe checks', () => {
    cy.mount(UiStateBannerComponent, {
      componentProperties: {
        kind: 'error',
        title: 'Something went wrong',
        message: 'Please review the highlighted fields.',
        detail: 'Validation failed',
        fieldErrors: { email: ['Valid email is required.'] },
        testId: 'test-error-banner',
      },
    });

    cy.get('[data-testid="test-error-banner"]')
      .should('contain.text', 'Something went wrong')
      .and('contain.text', 'Valid email is required.');
    cy.injectAxe();
    cy.checkA11y();
  });

  it('renders loading, empty, and success states', () => {
    cy.mount(UiStateBannerComponent, {
      componentProperties: {
        kind: 'success',
        title: 'Saved',
        message: 'Your changes were saved.',
        testId: 'test-success-banner',
      },
    });

    cy.get('[data-testid="test-success-banner"]').should('contain.text', 'Your changes were saved.');
    cy.injectAxe();
    cy.checkA11y();
  });
});
