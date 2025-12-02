import { Routes } from '@angular/router';
import { HealthPageComponent } from './features/health/health-page.component';
import { PolicyListComponent } from './features/policies/policy-list/policy-list.component';
import { PolicyEditorComponent } from './features/policies/policy-editor/policy-editor.component';
import { PolicyHistoryComponent } from './features/policies/policy-history/policy-history.component';
import { PolicyOverridesComponent } from './features/policies/policy-overrides/policy-overrides.component';

export const routes: Routes = [
  { path: '', redirectTo: 'policies', pathMatch: 'full' },
  { path: 'policies', component: PolicyListComponent },
  { path: 'policies/:policyKey/history', component: PolicyHistoryComponent },
  { path: 'policies/:policyKey/overrides', component: PolicyOverridesComponent },
  { path: 'policies/new', component: PolicyEditorComponent },
  { path: 'health', component: HealthPageComponent }
];
