import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'applications', pathMatch: 'full' },
  {
    path: 'applications',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/applications/applications.routes').then((m) => m.APPLICATIONS_ROUTES),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadChildren: () => import('./features/profile/profile.routes').then((m) => m.PROFILE_ROUTES),
  },
  {
    path: 'cover-letters',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/cover-letters/cover-letters.routes').then((m) => m.COVER_LETTERS_ROUTES),
  },
  {
    path: 'reminders',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/reminders/reminders.routes').then((m) => m.REMINDERS_ROUTES),
  },
  {
    path: 'account',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/pages/account-settings/account-settings.component').then(
        (m) => m.AccountSettingsComponent,
      ),
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
];
