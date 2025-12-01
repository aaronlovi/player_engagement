import { Routes } from '@angular/router';
import { HealthPageComponent } from './health-page.component';
import { PolicyListComponent } from './policy-list.component';

export const routes: Routes = [
  { path: '', redirectTo: 'policies', pathMatch: 'full' },
  { path: 'policies', component: PolicyListComponent },
  { path: 'health', component: HealthPageComponent }
];
