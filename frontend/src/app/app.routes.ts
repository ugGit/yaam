import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'applications', pathMatch: 'full' },
  {
    path: 'applications',
    loadChildren: () =>
      import('./features/applications/applications.routes').then((m) => m.APPLICATIONS_ROUTES),
  },
  {
    path: 'profile',
    loadChildren: () => import('./features/profile/profile.routes').then((m) => m.PROFILE_ROUTES),
  },
  {
    path: 'cover-letters',
    loadChildren: () =>
      import('./features/cover-letters/cover-letters.routes').then((m) => m.COVER_LETTERS_ROUTES),
  },
  {
    path: 'reminders',
    loadChildren: () =>
      import('./features/reminders/reminders.routes').then((m) => m.REMINDERS_ROUTES),
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
];
