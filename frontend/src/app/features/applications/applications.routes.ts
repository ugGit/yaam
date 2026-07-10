import { Routes } from '@angular/router';
import { ApplicationListComponent } from './pages/application-list/application-list.component';
import { ApplicationDetailComponent } from './pages/application-detail/application-detail.component';

export const APPLICATIONS_ROUTES: Routes = [
  { path: '', component: ApplicationListComponent },
  { path: 'new', component: ApplicationDetailComponent },
  { path: ':id', component: ApplicationDetailComponent },
];
