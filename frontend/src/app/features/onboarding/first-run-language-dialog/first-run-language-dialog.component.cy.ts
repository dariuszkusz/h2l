import { provideStore } from '@ngrx/store';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { FirstRunLanguageDialogComponent } from './first-run-language-dialog.component';

describe('FirstRunLanguageDialogComponent', () => {
  it('renders language options, emits preview and confirm, and passes axe checks', () => {
    const onPreview = cy.stub().as('previewSpy');
    const onConfirm = cy.stub().as('confirmSpy');

    cy.mount(FirstRunLanguageDialogComponent, {
      componentProperties: {
        selectedLanguage: 'en',
        languagePreviewed: { emit: onPreview } as never,
        languageConfirmed: { emit: onConfirm } as never,
      },
      providers: [provideStore({ i18n: i18nReducer })],
    });

    cy.get('[data-testid="first-run-language-dialog"]').should('be.visible');
    cy.injectAxe();
    cy.checkA11y();

    cy.get('[data-testid="first-run-lang-pl"]').click();
    cy.get('@previewSpy').should('have.been.calledWith', 'pl');

    cy.get('[data-testid="first-run-lang-confirm"]').click();
    cy.get('@confirmSpy').should('have.been.calledWith', 'en');
  });
});
