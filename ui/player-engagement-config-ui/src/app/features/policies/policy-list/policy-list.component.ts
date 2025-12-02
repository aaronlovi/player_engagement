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
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly busyKeys = signal<Set<string>>(new Set());

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

    const result: ApiResult<unknown> = await this.api.listPolicyVersions(policyKey, {
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

    this.items.set(this.normalizeItems(policyKey, result.body));
    this.actionMessage.set(null);
  }

  protected statusClass(status: string): string {
    const normalized = this.normalizeStatus(status).toLowerCase();
    switch (normalized) {
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

  protected canPublish(status: string): boolean {
    const normalized = this.normalizeStatus(status).toLowerCase();
    return normalized === 'draft' || normalized === 'archived';
  }

  protected canRetire(status: string): boolean {
    return this.normalizeStatus(status).toLowerCase() === 'published';
  }

  protected isBusy(item: PolicyListItemDto): boolean {
    return this.busyKeys().has(this.actionKey(item));
  }

  async publish(item: PolicyListItemDto): Promise<void> {
    if (!this.canPublish(item.version.status)) return;
    if (!window.confirm(`Publish version ${item.version.policyVersion} for policy ${item.policyKey}?`)) return;

    const key = this.actionKey(item);
    this.setBusy(key, true);
    this.error.set(null);
    this.actionMessage.set(null);

    const result = await this.api.publishPolicyVersion(item.policyKey, item.version.policyVersion, {});
    this.setBusy(key, false);
    this.lastStatus.set(result.status);

    if (!result.ok) {
      this.error.set(result.error ?? 'Publish failed');
      return;
    }

    this.actionMessage.set(`Published version ${item.version.policyVersion} for ${item.policyKey}.`);
    await this.fetch();
  }

  async retire(item: PolicyListItemDto): Promise<void> {
    if (!this.canRetire(item.version.status)) return;
    if (!window.confirm(`Retire version ${item.version.policyVersion} for policy ${item.policyKey}?`)) return;

    const key = this.actionKey(item);
    this.setBusy(key, true);
    this.error.set(null);
    this.actionMessage.set(null);

    const result = await this.api.retirePolicyVersion(item.policyKey, item.version.policyVersion, {});
    this.setBusy(key, false);
    this.lastStatus.set(result.status);

    if (!result.ok) {
      this.error.set(result.error ?? 'Retire failed');
      return;
    }

    this.actionMessage.set(`Retired version ${item.version.policyVersion} for ${item.policyKey}.`);
    await this.fetch();
  }

  private actionKey(item: PolicyListItemDto): string {
    return `${item.policyKey}:${item.version.policyVersion}`;
  }

  private setBusy(key: string, busy: boolean): void {
    const next = new Set(this.busyKeys());
    if (busy) {
      next.add(key);
    } else {
      next.delete(key);
    }

    this.busyKeys.set(next);
  }

  private normalizeItems(policyKey: string, body: unknown): PolicyListItemDto[] {
    if (!Array.isArray(body)) return [];

    return body.map(item => {
      if (item && typeof item === 'object' && 'version' in item && (item as any).version) {
        const typed = item as PolicyListItemDto;
        typed.version.status = this.normalizeStatus(typed.version.status) as any;
        return typed;
      }

      const raw = item as any;
      const version = raw as Partial<PolicyListItemDto['version']>;
      return {
        policyKey: raw.policyKey ?? policyKey,
        displayName: raw.displayName ?? '',
        description: raw.description ?? '',
        version: {
          policyVersion: version.policyVersion ?? 0,
          status: this.normalizeStatus(version.status) as any,
          baseXpAmount: version.baseXpAmount ?? 0,
          currency: version.currency ?? '',
          claimWindowStartOffset: version.claimWindowStartOffset ?? '',
          claimWindowDuration: version.claimWindowDuration ?? '',
          anchorStrategy: version.anchorStrategy ?? '',
          graceAllowedMisses: version.graceAllowedMisses ?? 0,
          graceWindowDays: version.graceWindowDays ?? 0,
          streakModelType: version.streakModelType ?? '',
          streakModelParameters: version.streakModelParameters ?? {},
          previewSampleWindowDays: version.previewSampleWindowDays ?? 0,
          previewDefaultSegment: version.previewDefaultSegment ?? null,
          effectiveAt: version.effectiveAt ?? null,
          supersededAt: version.supersededAt ?? null,
          createdAt: version.createdAt ?? '',
          createdBy: version.createdBy ?? '',
          publishedAt: version.publishedAt ?? null
        }
      };
    });
  }

  private normalizeStatus(status: any): string {
    if (status === null || status === undefined) return 'Draft';
    if (typeof status === 'number') {
      switch (status) {
        case 1:
          return 'Draft';
        case 2:
          return 'Published';
        case 3:
          return 'Archived';
        default:
          return 'Draft';
      }
    }

    const text = status.toString();
    if (!text) return 'Draft';
    if (/^published$/i.test(text)) return 'Published';
    if (/^archived$/i.test(text)) return 'Archived';
    return 'Draft';
  }
}
