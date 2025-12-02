import { CommonModule } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { PolicyApiService, PolicyListItemDto } from '../../../core/api/policy-api.service';
import { PolicyVersionStatus } from '../../../core/api/policy-types';
import { ApiResult } from '../../../core/utils/http';

type StatusFilter = '' | PolicyVersionStatus;
type PolicyListForm = {
  policyKey: FormControl<string>;
  status: FormControl<StatusFilter>;
  limit: FormControl<number>;
};

@Component({
  selector: 'app-policy-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './policy-list.component.html',
  styleUrls: ['./policy-list.component.scss']
})
export class PolicyListComponent {
  readonly form: FormGroup<PolicyListForm>;

  protected readonly loading = signal(false);
  protected readonly items = signal<PolicyListItemDto[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly lastStatus = signal<number | null>(null);

  protected readonly hasResults = computed(() => this.items().length > 0);

  constructor(
    private readonly api: PolicyApiService,
    fb: FormBuilder
  ) {
    this.form = fb.nonNullable.group<PolicyListForm>({
      policyKey: fb.nonNullable.control('', [Validators.required, Validators.minLength(3)]),
      status: fb.nonNullable.control<StatusFilter>('', []),
      limit: fb.nonNullable.control(25, [Validators.min(1), Validators.max(200)])
    });
  }

  async fetch(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.lastStatus.set(null);

    const { policyKey, status, limit } = this.form.getRawValue();

    const result: ApiResult<PolicyListItemDto[]> = await this.api.listPolicyVersions(policyKey, {
      status: status || undefined,
      limit: limit ?? undefined
    });

    this.loading.set(false);
    this.lastStatus.set(result.status);

    if (!result.ok || !result.body) {
      this.error.set(result.error ?? 'Request failed');
      this.items.set([]);
      return;
    }

    this.items.set(result.body);
  }

  protected statusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'published':
        return 'badge badge--ok';
      case 'draft':
        return 'badge';
      case 'archived':
        return 'badge badge--warn';
      default:
        return 'badge';
    }
  }
}
