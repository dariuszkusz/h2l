import { TestBed } from '@angular/core/testing';
import { provideStore } from '@ngrx/store';
import { beforeEach, describe, expect, it } from 'vitest';
import { i18nReducer } from '../../../core/i18n/store/i18n.reducer';
import { BrandLogoComponent } from './brand-logo.component';

describe('BrandLogoComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrandLogoComponent],
      providers: [provideStore({ i18n: i18nReducer })],
    }).compileComponents();
  });

  it('renders full logo (mark + wordmark + optional tagline)', () => {
    const fixture = TestBed.createComponent(BrandLogoComponent);
    fixture.componentRef.setInput('variant', 'full');
    fixture.componentRef.setInput('showTagline', true);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="brand-logo-mark"]')?.getAttribute('aria-hidden')).toBe('true');
    expect(el.querySelector('[data-testid="brand-logo-wordmark"]')?.textContent).toContain('h2l');
    expect(el.querySelector('[data-testid="brand-logo-tagline"]')).not.toBeNull();
  });

  it('renders mark-only variant with accessible role="img" and aria-label', () => {
    const fixture = TestBed.createComponent(BrandLogoComponent);
    fixture.componentRef.setInput('variant', 'mark');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const mark = el.querySelector('[data-testid="brand-logo-mark"]');
    expect(mark).not.toBeNull();
    expect(mark?.getAttribute('role')).toBe('img');
    expect(mark?.getAttribute('aria-label')).toBe('h2l');
    expect(el.querySelector('[data-testid="brand-logo-wordmark"]')).toBeNull();
  });

  it('renders wordmark-only variant without the mark', () => {
    const fixture = TestBed.createComponent(BrandLogoComponent);
    fixture.componentRef.setInput('variant', 'wordmark');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[data-testid="brand-logo-mark"]')).toBeNull();
    expect(el.querySelector('[data-testid="brand-logo-wordmark"]')?.textContent).toContain('h2l');
  });
});
