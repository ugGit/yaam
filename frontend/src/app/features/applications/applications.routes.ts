import { Routes } from '@angular/router';
import { ApplicationListComponent } from './pages/application-list/application-list.component';

export const APPLICATIONS_ROUTES: Routes = [
  { path: '', component: ApplicationListComponent },
];
