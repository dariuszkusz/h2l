import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { App } from './app';
import { AuthEffects } from './core/auth/store/auth.effects';
import { authReducer } from './core/auth/store/auth.reducer';
import { ConnectivityEffects } from './core/connectivity/store/connectivity.effects';
import { connectivityReducer } from './core/connectivity/store/connectivity.reducer';
import { I18nEffects } from './core/i18n/store/i18n.effects';
import { i18nReducer } from './core/i18n/store/i18n.reducer';
import { SetupEffects } from './core/setup/store/setup.effects';
import { setupReducer } from './core/setup/store/setup.reducer';
import { ThemeEffects } from './core/theme/store/theme.effects';
import { themeReducer } from './core/theme/store/theme.reducer';

describe('App', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    globalThis.localStorage?.clear();
    globalThis.sessionStorage?.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideStore({
          i18n: i18nReducer,
          theme: themeReducer,
          connectivity: connectivityReducer,
          auth: authReducer,
          setup: setupReducer,
        }),
        provideEffects([
          I18nEffects,
          ThemeEffects,
          ConnectivityEffects,
          AuthEffects,
          SetupEffects,
        ]),
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('creates the app shell, checks setup status, and displays first-run language dialog when unset', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const setupReq = httpMock.expectOne('/api/v1/setup/status');
    setupReq.flush({
      isInitialized: true,
      instanceId: '01900000-0000-7000-8000-000000000001',
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[data-testid="first-run-language-dialog"]')).not.toBeNull();
    expect(compiled.querySelector('[data-testid="connectivity-banner"]')).not.toBeNull();
    expect(compiled.querySelector('[data-testid="nav-login"]')).not.toBeNull();
  });
});
