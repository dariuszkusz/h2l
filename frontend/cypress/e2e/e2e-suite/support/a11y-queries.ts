export type TextMatcher = string | RegExp;

export type SemanticRole =
  | 'alert'
  | 'alertdialog'
  | 'button'
  | 'checkbox'
  | 'combobox'
  | 'dialog'
  | 'heading'
  | 'link'
  | 'listbox'
  | 'main'
  | 'navigation'
  | 'option'
  | 'radio'
  | 'radiogroup'
  | 'row'
  | 'status'
  | 'switch'
  | 'tab'
  | 'table'
  | 'tablist'
  | 'tabpanel'
  | 'textbox';

const ROLE_SELECTOR_MAP: Record<SemanticRole, string> = {
  alert: '[role="alert"]',
  alertdialog: '[role="alertdialog"], dialog[role="alertdialog"]',
  button: 'button, [role="button"], input[type="button"], input[type="submit"]',
  checkbox: 'input[type="checkbox"], [role="checkbox"]',
  combobox: 'select, [role="combobox"]',
  dialog: 'dialog, [role="dialog"], [role="alertdialog"]',
  heading: 'h1, h2, h3, h4, h5, h6, [role="heading"]',
  link: 'a[href], [role="link"]',
  listbox: '[role="listbox"]',
  main: 'main, [role="main"]',
  navigation: 'nav, [role="navigation"]',
  option: 'option, [role="option"]',
  radio: 'input[type="radio"], [role="radio"]',
  radiogroup: '[role="radiogroup"], fieldset',
  row: 'tr, [role="row"]',
  status: '[role="status"], output',
  switch: '[role="switch"]',
  tab: '[role="tab"]',
  table: 'table, [role="table"], [role="grid"]',
  tablist: '[role="tablist"]',
  tabpanel: '[role="tabpanel"]',
  textbox:
    'input:not([type="hidden"]):not([type="checkbox"]):not([type="radio"]):not([type="button"]):not([type="submit"]), textarea, [role="textbox"]',
};

function normalizeText(value: string | null | undefined): string {
  return (value ?? '').replace(/\s+/g, ' ').trim();
}

function escapeAttrValue(value: string): string {
  return value.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

function getLabelTextWithoutControls(label: Element): string {
  const clone = label.cloneNode(true) as Element;
  const nestedControls = clone.querySelectorAll('input, select, textarea, button');
  nestedControls.forEach((node) => node.remove());
  return normalizeText(clone.textContent);
}

export function getAccessibleName(el: HTMLElement): string {
  const ariaLabel = normalizeText(el.getAttribute('aria-label'));
  if (ariaLabel.length > 0) {
    return ariaLabel;
  }

  const doc = el.ownerDocument;
  const labelledBy = normalizeText(el.getAttribute('aria-labelledby'));
  if (labelledBy.length > 0) {
    const labelParts = labelledBy
      .split(/\s+/)
      .map((id) => normalizeText(doc.getElementById(id)?.textContent))
      .filter((part) => part.length > 0);
    if (labelParts.length > 0) {
      return labelParts.join(' ');
    }
  }

  const tagName = el.tagName.toLowerCase();
  if (tagName === 'input' || tagName === 'select' || tagName === 'textarea') {
    const id = el.getAttribute('id');
    if (id !== null && id.length > 0) {
      const explicitLabel = doc.querySelector(`label[for="${escapeAttrValue(id)}"]`);
      if (explicitLabel !== null) {
        const labelText = getLabelTextWithoutControls(explicitLabel);
        if (labelText.length > 0) {
          return labelText;
        }
      }
    }

    const parentLabel = el.closest('label');
    if (parentLabel !== null) {
      const labelText = getLabelTextWithoutControls(parentLabel);
      if (labelText.length > 0) {
        return labelText;
      }
    }
  }

  if (tagName === 'input') {
    const inputEl = el as HTMLInputElement;
    if (inputEl.type === 'submit' || inputEl.type === 'button') {
      const value = normalizeText(inputEl.value);
      if (value.length > 0) {
        return value;
      }
    }
  }

  const text = normalizeText(el.textContent);
  if (text.length > 0) {
    return text;
  }

  const title = normalizeText(el.getAttribute('title'));
  if (title.length > 0) {
    return title;
  }

  return '';
}

export function matchesName(candidate: string, matcher?: TextMatcher): boolean {
  if (matcher === undefined) {
    return true;
  }
  const normalized = normalizeText(candidate);
  if (typeof matcher === 'string') {
    return normalized.toLowerCase().includes(matcher.toLowerCase());
  }
  if (matcher.global) {
    matcher.lastIndex = 0;
  }
  return matcher.test(normalized);
}

export function byRole(
  role: SemanticRole,
  nameMatcher?: TextMatcher,
): Cypress.Chainable<JQuery<HTMLElement>> {
  const selector = ROLE_SELECTOR_MAP[role];
  const base = cy.get<HTMLElement>(selector);
  if (nameMatcher === undefined) {
    return base;
  }
  return base.filter((_index, el) => matchesName(getAccessibleName(el), nameMatcher));
}

export function byLabel(labelMatcher: TextMatcher): Cypress.Chainable<JQuery<HTMLElement>> {
  const selector =
    'input:not([type="hidden"]), select, textarea, [role="textbox"], [role="combobox"], [role="checkbox"], [role="switch"], [role="radio"]';
  return cy.get<HTMLElement>(selector).filter((_index, el) => {
    const name = getAccessibleName(el);
    return matchesName(name, labelMatcher);
  });
}

export function byButton(nameMatcher: TextMatcher): Cypress.Chainable<JQuery<HTMLElement>> {
  return byRole('button', nameMatcher);
}

export function byLink(nameMatcher: TextMatcher): Cypress.Chainable<JQuery<HTMLElement>> {
  return byRole('link', nameMatcher);
}

export function byHeading(nameMatcher?: TextMatcher): Cypress.Chainable<JQuery<HTMLElement>> {
  return byRole('heading', nameMatcher);
}

export function byAlert(textMatcher?: TextMatcher): Cypress.Chainable<JQuery<HTMLElement>> {
  return byRole('alert', textMatcher);
}

export function byStatus(textMatcher?: TextMatcher): Cypress.Chainable<JQuery<HTMLElement>> {
  return byRole('status', textMatcher);
}

export function byDialog(nameMatcher?: TextMatcher): Cypress.Chainable<JQuery<HTMLElement>> {
  return byRole('dialog', nameMatcher);
}

export function byLiveRegion(textMatcher?: TextMatcher): Cypress.Chainable<JQuery<HTMLElement>> {
  const base = cy.get<HTMLElement>('[aria-live="polite"], [aria-live="assertive"], [role="alert"], [role="status"]');
  if (textMatcher === undefined) {
    return base;
  }
  return base.filter((_index, el) => matchesName(el.textContent ?? '', textMatcher));
}

export function fillByLabel(
  labelMatcher: TextMatcher,
  value: string,
): Cypress.Chainable<JQuery<HTMLElement>> {
  return byLabel(labelMatcher).first().clear().type(value);
}

export function assertInvalidField(
  labelMatcher: TextMatcher,
  errorTextMatcher?: TextMatcher,
): void {
  byLabel(labelMatcher)
    .first()
    .should('have.attr', 'aria-invalid', 'true')
    .then(($input) => {
      const describedBy = normalizeText($input.attr('aria-describedby'));
      expect(describedBy.length, 'aria-describedby must reference an error description element').to.be.greaterThan(0);

      const doc = $input.get(0).ownerDocument;
      const describedText = describedBy
        .split(/\s+/)
        .map((id) => normalizeText(doc.getElementById(id)?.textContent))
        .filter((part) => part.length > 0)
        .join(' ');

      expect(describedText.length, 'Referenced aria-describedby error message must be non-empty').to.be.greaterThan(0);
      if (errorTextMatcher !== undefined) {
        expect(matchesName(describedText, errorTextMatcher)).to.eq(true);
      }
    });
}
