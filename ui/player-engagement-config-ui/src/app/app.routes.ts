import { Routes } from '@angular/router';
import { HealthPageComponent } from './features/health/health-page.component';
import { PolicyListComponent } from './features/policies/policy-list/policy-list.component';

export const routes: Routes = [
  { path: '', redirectTo: 'policies', pathMatch: 'full' },
  { path: 'policies', component: PolicyListComponent },
  { path: 'health', component: HealthPageComponent }
];
