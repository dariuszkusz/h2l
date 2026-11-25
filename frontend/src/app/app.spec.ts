import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { beforeEach, describe, expect, it } from 'vitest';
import { App } from './app';
import { ConnectivityEffects } from './core/connectivity/store/connectivity.effects';
import { connectivityReducer } from './core/connectivity/store/connectivity.reducer';
import { I18nEffects } from './core/i18n/store/i18n.effects';
import { i18nReducer } from './core/i18n/store/i18n.reducer';
import { ThemeEffects } from './core/theme/store/theme.effects';
import { themeReducer } from './core/theme/store/theme.reducer';

describe('App', () => {
  beforeEach(async () => {
    globalThis.localStorage?.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideStore({
          i18n: i18nReducer,
          theme: themeReducer,
          connectivity: connectivityReducer,
        }),
        provideEffects([I18nEffects, ThemeEffects, ConnectivityEffects]),
      ],
    }).compileComponents();
  });

  it('creates the app shell and displays first-run language dialog when unset', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="first-run-language-dialog"]')).not.toBeNull();
    expect(compiled.querySelector('[data-testid="connectivity-banner"]')).not.toBeNull();
  });
});
