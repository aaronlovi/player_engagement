import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';

import { PolicyApiService, PolicyListItemDto } from '../../../core/api/policy-api.service';
import { PolicyVersionStatus } from '../../../core/api/policy-types';
import { ApiState, createInitialState, toApiState } from '../../../core/utils/http';

type HistoryForm = {
  policyKey: FormControl<string>;
  limit: FormControl<number>;
};

type DiffField = {
  label: string;
  path: keyof PolicyListItemDto['version'];
};

@Component({
  selector: 'app-policy-history',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './policy-history.component.html',
  styleUrls: ['./policy-history.component.scss']
})
export class PolicyHistoryComponent implements OnInit, OnDestroy {
  readonly form: FormGroup<HistoryForm>;

  protected readonly state = signal<ApiState<unknown>>(createInitialState());
  protected readonly versions = signal<PolicyListItemDto[]>([]);
  protected readonly selectedA = signal<PolicyListItemDto | null>(null);
  protected readonly selectedB = signal<PolicyListItemDto | null>(null);

  readonly fields: DiffField[] = [
    { label: 'Status', path: 'status' },
    { label: 'Base XP', path: 'baseXpAmount' },
    { label: 'Currency', path: 'currency' },
    { label: 'Claim window start', path: 'claimWindowStartOffset' },
    { label: 'Claim window duration', path: 'claimWindowDuration' },
    { label: 'Anchor strategy', path: 'anchorStrategy' },
    { label: 'Grace allowed misses', path: 'graceAllowedMisses' },
    { label: 'Grace window days', path: 'graceWindowDays' },
    { label: 'Streak model type', path: 'streakModelType' },
    { label: 'Preview window days', path: 'previewSampleWindowDays' },
    { label: 'Preview default segment', path: 'previewDefaultSegment' },
    { label: 'Effective at', path: 'effectiveAt' },
    { label: 'Published at', path: 'publishedAt' },
    { label: 'Created at', path: 'createdAt' },
    { label: 'Created by', path: 'createdBy' }
  ];

  private paramSub?: Subscription;

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: PolicyApiService,
    private readonly route: ActivatedRoute
  ) {
    this.form = fb.nonNullable.group<HistoryForm>({
      policyKey: fb.nonNullable.control('', [Validators.required, Validators.minLength(3)]),
      limit: fb.nonNullable.control(25, [Validators.min(1), Validators.max(200)])
    });
  }

  ngOnInit(): void {
    this.paramSub = this.route.paramMap.subscribe(params => {
      const key = params.get('policyKey');
      if (key) {
        this.form.controls.policyKey.setValue(key);
        void this.fetch();
      }
    });
  }

  ngOnDestroy(): void {
    this.paramSub?.unsubscribe();
  }

  async fetch(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.state.set({ ...createInitialState(), loading: true });
    this.versions.set([]);
    this.selectedA.set(null);
    this.selectedB.set(null);

    const previousA = this.selectedA()?.version.policyVersion;
    const previousB = this.selectedB()?.version.policyVersion;

    const { policyKey, limit } = this.form.getRawValue();
    const result = await this.api.listPolicyVersions(policyKey, { limit });
    this.state.set(toApiState(result));

    if (!result.ok || !result.body) {
      return;
    }

    const items = this.normalizeItems(policyKey, result.body);
    this.versions.set(items);

    const matchA = previousA ? items.find(v => v.version.policyVersion === previousA) : null;
    const matchB = previousB ? items.find(v => v.version.policyVersion === previousB) : null;

    this.selectedA.set(matchA ?? items.at(0) ?? null);
    this.selectedB.set(matchB ?? items.at(1) ?? matchA ?? items.at(0) ?? null);
  }

  onSelectA(value: string): void {
    const parsed = Number(value);
    const match = this.versions().find(v => v.version.policyVersion === parsed);
    this.selectedA.set(match ?? null);
  }

  onSelectB(value: string): void {
    const parsed = Number(value);
    const match = this.versions().find(v => v.version.policyVersion === parsed);
    this.selectedB.set(match ?? null);
  }

  selectedAId(): string | number {
    return this.selectedA()?.version.policyVersion ?? '';
  }

  selectedBId(): string | number {
    return this.selectedB()?.version.policyVersion ?? '';
  }

  private normalizeItems(policyKey: string, body: unknown): PolicyListItemDto[] {
    if (!Array.isArray(body)) return [];

    return body.map(raw => {
      const source = (raw as any)?.version ?? raw;
      const version = source as Partial<PolicyListItemDto['version']>;

      return {
        policyKey: (raw as any)?.policyKey ?? policyKey,
        displayName: (raw as any)?.displayName ?? '',
        description: (raw as any)?.description ?? '',
        version: {
          policyVersion: Number(version.policyVersion ?? 0),
          status: this.normalizeStatus(version.status) as PolicyVersionStatus,
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

  diffClass(field: DiffField): string {
    const a = this.selectedA();
    const b = this.selectedB();
    if (!a || !b) return '';

    const va = this.readValue(a, field.path);
    const vb = this.readValue(b, field.path);
    return va === vb ? '' : 'diff';
  }

  readValue(item: PolicyListItemDto, path: keyof PolicyListItemDto['version']): unknown {
    return (item?.version as any)?.[path];
  }
}
