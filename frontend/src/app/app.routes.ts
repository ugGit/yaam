import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './core/layout/shell/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    canActivateChild: [authGuard],
    children: [
      { path: '', redirectTo: 'applications', pathMatch: 'full' },
      {
        path: 'applications',
        loadChildren: () =>
          import('./features/applications/applications.routes').then((m) => m.APPLICATIONS_ROUTES),
      },
      {
        path: 'profile',
        loadChildren: () =>
          import('./features/profile/profile.routes').then((m) => m.PROFILE_ROUTES),
      },
      {
        path: 'cover-letters',
        loadChildren: () =>
          import('./features/cover-letters/cover-letters.routes').then(
            (m) => m.COVER_LETTERS_ROUTES,
          ),
      },
      {
        path: 'reminders',
        loadChildren: () =>
          import('./features/reminders/reminders.routes').then((m) => m.REMINDERS_ROUTES),
      },
      {
        path: 'account',
        loadComponent: () =>
          import('./features/auth/pages/account-settings/account-settings.component').then(
            (m) => m.AccountSettingsComponent,
          ),
      },
    ],
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  { path: '**', redirectTo: 'applications' },
];
