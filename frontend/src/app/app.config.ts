import { provideHttpClient, withInterceptors } from '@angular/common/http';
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
import { jwtInterceptor } from './core/auth/interceptors/jwt.interceptor';
import { AuthEffects } from './core/auth/store/auth.effects';
import { authReducer } from './core/auth/store/auth.reducer';
import { ConnectivityEffects } from './core/connectivity/store/connectivity.effects';
import { connectivityReducer } from './core/connectivity/store/connectivity.reducer';
import { I18nEffects } from './core/i18n/store/i18n.effects';
import { i18nReducer } from './core/i18n/store/i18n.reducer';
import { LicensingEffects } from './core/licensing/store/licensing.effects';
import { licensingReducer } from './core/licensing/store/licensing.reducer';
import { SetupEffects } from './core/setup/store/setup.effects';
import { setupReducer } from './core/setup/store/setup.reducer';
import { ThemeEffects } from './core/theme/store/theme.effects';
import { themeReducer } from './core/theme/store/theme.reducer';
import { AdminProjectsEffects } from './features/admin-projects/store/admin-projects.effects';
import { adminProjectsReducer } from './features/admin-projects/store/admin-projects.reducer';
import { AdminUsersEffects } from './features/admin-users/store/admin-users.effects';
import { adminUsersReducer } from './features/admin-users/store/admin-users.reducer';
import { ConnectorsEffects } from './features/connectors/store/connectors.effects';
import { connectorsReducer } from './features/connectors/store/connectors.reducer';
import { FormBuilderEffects } from './features/form-builder/store/form-builder.effects';
import { formBuilderReducer } from './features/form-builder/store/form-builder.reducer';
import { TasksEffects } from './features/tasks/store/tasks.effects';
import { tasksReducer } from './features/tasks/store/tasks.reducer';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([jwtInterceptor])),
    provideStore({
      i18n: i18nReducer,
      theme: themeReducer,
      connectivity: connectivityReducer,
      auth: authReducer,
      setup: setupReducer,
      licensing: licensingReducer,
      adminUsers: adminUsersReducer,
      adminProjects: adminProjectsReducer,
      connectors: connectorsReducer,
      formBuilder: formBuilderReducer,
      tasks: tasksReducer,
    }),
    provideEffects([
      I18nEffects,
      ThemeEffects,
      ConnectivityEffects,
      AuthEffects,
      SetupEffects,
      LicensingEffects,
      AdminUsersEffects,
      AdminProjectsEffects,
      ConnectorsEffects,
      FormBuilderEffects,
      TasksEffects,
    ]),
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
