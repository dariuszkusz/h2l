import { Routes } from '@angular/router';
import { authGuard } from './core/auth/guards/auth.guard';
import { guestOnlyGuard } from './core/auth/guards/guest-only.guard';
import { roleGuard } from './core/auth/guards/role.guard';
import { setupNotInitializedGuard } from './core/setup/guards/setup-not-initialized.guard';
import { setupRequiredGuard } from './core/setup/guards/setup-required.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'tasks',
  },
  {
    path: 'setup',
    canActivate: [setupNotInitializedGuard],
    loadComponent: () =>
      import('./features/setup/setup-page/setup-page.component').then(
        (m) => m.SetupPageComponent,
      ),
  },
  {
    path: 'login',
    canActivate: [setupRequiredGuard, guestOnlyGuard],
    loadComponent: () =>
      import('./features/auth/login-page/login-page.component').then(
        (m) => m.LoginPageComponent,
      ),
  },
  {
    path: 'activate',
    canActivate: [setupRequiredGuard],
    loadComponent: () =>
      import('./features/auth/activate-page/activate-page.component').then(
        (m) => m.ActivatePageComponent,
      ),
  },
  {
    path: 'tasks',
    canActivate: [setupRequiredGuard, authGuard],
    loadComponent: () =>
      import('./features/tasks/tasks-page/tasks-page.component').then(
        (m) => m.TasksPageComponent,
      ),
  },
  {
    path: 'tasks/:taskId',
    canActivate: [setupRequiredGuard, authGuard],
    loadComponent: () =>
      import('./features/tasks/task-detail-page/task-detail-page.component').then(
        (m) => m.TaskDetailPageComponent,
      ),
  },
  {
    path: 'settings',
    canActivate: [setupRequiredGuard, authGuard],
    loadComponent: () =>
      import('./features/settings/settings-page/settings-page.component').then(
        (m) => m.SettingsPageComponent,
      ),
  },
  {
    path: 'admin/users',
    canActivate: [setupRequiredGuard, authGuard, roleGuard(['Admin'])],
    loadComponent: () =>
      import('./features/admin-users/admin-users-page/admin-users-page.component').then(
        (m) => m.AdminUsersPageComponent,
      ),
  },
  {
    path: 'admin/projects',
    canActivate: [setupRequiredGuard, authGuard, roleGuard(['Admin', 'ProcessDesigner'])],
    loadComponent: () =>
      import('./features/admin-projects/admin-projects-page/admin-projects-page.component').then(
        (m) => m.AdminProjectsPageComponent,
      ),
  },
  {
    path: 'admin/forms/:formId/builder',
    canActivate: [setupRequiredGuard, authGuard, roleGuard(['Admin', 'ProcessDesigner'])],
    loadComponent: () =>
      import('./features/form-builder/form-builder-page/form-builder-page.component').then(
        (m) => m.FormBuilderPageComponent,
      ),
  },
  {
    path: '**',
    redirectTo: 'tasks',
  },
];
