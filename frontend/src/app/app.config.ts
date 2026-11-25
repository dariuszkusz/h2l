import { provideHttpClient } from '@angular/common/http';
import {
  ApplicationConfig,
  isDevMode,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { routes } from './app.routes';
import { ConnectivityEffects } from './core/connectivity/store/connectivity.effects';
import { connectivityReducer } from './core/connectivity/store/connectivity.reducer';
import { I18nEffects } from './core/i18n/store/i18n.effects';
import { i18nReducer } from './core/i18n/store/i18n.reducer';
import { ThemeEffects } from './core/theme/store/theme.effects';
import { themeReducer } from './core/theme/store/theme.reducer';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(),
    provideStore({
      i18n: i18nReducer,
      theme: themeReducer,
      connectivity: connectivityReducer,
    }),
    provideEffects([I18nEffects, ThemeEffects, ConnectivityEffects]),
    provideStoreDevtools({
      maxAge: 25,
      logOnly: !isDevMode(),
    }),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
