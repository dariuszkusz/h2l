import { provideStore } from '@ngrx/store';
import { components } from '../../../core/api/generated/openapi.types';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { FormRendererComponent } from './form-renderer.component';

type FormSchema = components['schemas']['FormSchema'];

const sampleSchema: FormSchema = {
  schemaVersion: '1.0',
  defaultLanguage: 'en',
  layoutMode: 'single',
  steps: [
    {
      id: 'step_1',
      title: {
        en: 'Software Purchase',
        de: 'Softwarekauf',
        pl: 'Zakup oprogramowania',
      },
      components: [
        {
          id: 'cmp_name',
          type: 'text',
          variableKey: 'softwareName',
          label: { en: 'Software name', de: 'Softwarename', pl: 'Nazwa oprogramowania' },
          preFilled: true,
          readOnly: true,
        },
        {
          id: 'cmp_cost',
          type: 'number',
          variableKey: 'estimatedCost',
          label: { en: 'Estimated cost', de: 'Geschätzte Kosten', pl: 'Szacowany koszt' },
          validators: [{ type: 'required' }, { type: 'min', value: 1 }],
        },
        {
          id: 'cmp_just',
          type: 'textarea',
          variableKey: 'businessJustification',
          label: { en: 'Justification', de: 'Begründung', pl: 'Uzasadnienie' },
          rules: {
            visibleIf: 'estimatedCost > 1000',
            requiredIf: 'estimatedCost > 1000',
          },
          validators: [
            {
              type: 'expression',
              expression:
                'estimatedCost <= 1000 || (businessJustification && businessJustification.length >= 10)',
              message: {
                en: 'Provide at least 10 characters.',
                de: 'Gib mindestens 10 Zeichen ein.',
                pl: 'Podaj co najmniej 10 znaków.',
              },
            },
          ],
        },
      ],
    },
  ],
};

describe('FormRendererComponent', () => {
  it('renders preFilled/readOnly, evaluates conditional visibleIf & AST expression, and passes axe', () => {
    const onSubmit = cy.spy().as('onSubmit');

    cy.mount(FormRendererComponent, {
      providers: [provideStore({ i18n: i18nReducer })],
      componentProperties: {
        schema: sampleSchema,
        language: 'pl',
        prefilledData: { softwareName: 'JetBrains Rider', estimatedCost: 500 },
        formSubmitted: { emit: onSubmit } as unknown as FormRendererComponent['formSubmitted'],
      },
    });

    cy.get('[data-testid="field-softwareName"]')
      .should('have.value', 'JetBrains Rider')
      .and('have.attr', 'readonly');

    cy.get('[data-testid="field-businessJustification"]').should('not.exist');

    cy.get('[data-testid="field-estimatedCost"]').clear().type('1500');
    cy.get('[data-testid="field-businessJustification"]').should('exist').type('short');

    cy.get('[data-testid="form-renderer-submit-btn"]').click();
    cy.contains('Podaj co najmniej 10 znaków.').should('be.visible');
    cy.get('@onSubmit').should('not.have.been.called');

    cy.get('[data-testid="field-businessJustification"]')
      .clear()
      .type('Uzasadnienie biznesowe dla zakupu licencji');
    cy.get('[data-testid="form-renderer-submit-btn"]').click();
    cy.get('@onSubmit').should('have.been.calledOnce');

    cy.injectAxe();
    cy.checkA11y();
  });
});
