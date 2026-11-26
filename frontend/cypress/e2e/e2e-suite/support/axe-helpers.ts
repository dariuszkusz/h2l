import type { Result } from 'axe-core';
import type { ColorPalette, SupportedLanguage, ThemeMode } from '../mocks/mock-api.types';

export interface AxeCheckContext {
  route?: string;
  theme?: ThemeMode;
  palette?: ColorPalette;
  language?: SupportedLanguage;
  includeSelector?: string;
}

export function formatAxeViolations(violations: Result[], context?: AxeCheckContext): string {
  const prefixParts: string[] = [];
  if (context?.route !== undefined) {
    prefixParts.push(`route=${context.route}`);
  }
  if (context?.language !== undefined) {
    prefixParts.push(`lang=${context.language}`);
  }
  if (context?.theme !== undefined) {
    prefixParts.push(`theme=${context.theme}`);
  }
  if (context?.palette !== undefined) {
    prefixParts.push(`palette=${context.palette}`);
  }
  const contextLabel = prefixParts.length > 0 ? ` (${prefixParts.join(', ')})` : '';

  const details = violations
    .map((violation) => {
      const targets = violation.nodes
        .map((node) => `${node.target.join(', ')} (${node.failureSummary ?? ''})`)
        .join(' | ');
      return `[${violation.impact ?? 'unknown'}] ${violation.id}: ${violation.help} -> ${targets}`;
    })
    .join('\n');

  return `Accessibility violations detected${contextLabel}:\n${details}`;
}

export function injectAndCheckA11y(context?: AxeCheckContext): void {
  cy.injectAxe();
  cy.checkA11y(context?.includeSelector, undefined, (violations) => {
    throw new Error(formatAxeViolations(violations, context));
  });
}

export function assertHtmlAttributes(expected: {
  lang?: SupportedLanguage;
  theme?: ThemeMode;
  palette?: ColorPalette;
}): void {
  if (expected.lang !== undefined) {
    cy.get('html').should('have.attr', 'lang', expected.lang);
  }
  if (expected.theme !== undefined) {
    cy.get('html').should('have.attr', 'data-theme', expected.theme);
    if (expected.theme === 'dark') {
      cy.get('html').should('have.class', 'dark');
    }
  }
  if (expected.palette !== undefined) {
    cy.get('html').should('have.attr', 'data-palette', expected.palette);
  }
}

export function assertNoHorizontalOverflow(): void {
  cy.window().then((win) => {
    const docEl = win.document.documentElement;
    expect(
      docEl.scrollWidth,
      `Expected no horizontal overflow (scrollWidth=${docEl.scrollWidth}, innerWidth=${win.innerWidth})`,
    ).to.be.lte(win.innerWidth);
  });
}

export function assertMobileTouchTargets(minSizePx = 44): void {
  cy.get<HTMLElement>(
    'button:visible, a[href]:visible, [role="button"]:visible, input:visible, select:visible, [role="switch"]:visible',
  ).each(($el) => {
    const rect = $el.get(0).getBoundingClientRect();
    if (rect.width === 0 && rect.height === 0) {
      return;
    }
    const label = $el.attr('aria-label') ?? $el.text().trim() ?? $el.prop('tagName');
    expect(
      rect.width >= minSizePx && rect.height >= minSizePx,
      `Interactive element "${String(label)}" (${Math.round(rect.width)}x${Math.round(rect.height)}px) must meet ${minSizePx}x${minSizePx}px mobile touch target`,
    ).to.eq(true);
  });
}

export function assertNoRawTranslationKeys(): void {
  cy.get('body').then(($body) => {
    const text = $body.text();
    const rawKeyMatches = text.match(/\b(?:setup|auth|admin|settings|licensing|common|errors)\.[a-z0-9_.-]+\b/gi);
    expect(rawKeyMatches, 'No raw i18n translation keys may appear in visible DOM text').to.eq(null);
  });
}
