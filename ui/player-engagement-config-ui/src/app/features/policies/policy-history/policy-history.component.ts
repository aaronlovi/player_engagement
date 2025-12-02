import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';

import { PolicyApiService, PolicyListItemDto } from '../../../core/api/policy-api.service';
import { PolicyVersionStatus } from '../../../core/api/policy-types';
import { ApiState, createInitialState, toApiState } from '../../../core/utils/http';
import { ObjectUtils, JsonObject, DiffResult } from '../../../utils/objectUtils';

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
  protected readonly diffs = signal<DiffResult | null>(null);
  protected readonly diffKeys = signal<string[]>([]);

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

    // Capture previous selections BEFORE resetting
    const previousA = this.selectedA()?.version.policyVersion;
    const previousB = this.selectedB()?.version.policyVersion;

    this.state.set({ ...createInitialState(), loading: true });
    this.versions.set([]);
    this.selectedA.set(null);
    this.selectedB.set(null);

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
    this.clearDiff();
  }

  onSelectA(value: string): void {
    const parsed = Number(value);
    const match = this.versions().find(v => v.version.policyVersion === parsed);
    this.selectedA.set(match ?? null);
    this.clearDiff();
  }

  onSelectB(value: string): void {
    const parsed = Number(value);
    const match = this.versions().find(v => v.version.policyVersion === parsed);
    this.selectedB.set(match ?? null);
    this.clearDiff();
  }

  /** Clears the current diff results */
  clearDiff(): void {
    this.diffs.set(null);
    this.diffKeys.set([]);
  }

  /** Triggers diff computation - called by Compare button */
  compare(): void {
    this.computeDiffs();
  }

  selectedAId(): string {
    const id = this.selectedA()?.version.policyVersion;
    return id !== undefined ? String(id) : '';
  }

  selectedBId(): string {
    const id = this.selectedB()?.version.policyVersion;
    return id !== undefined ? String(id) : '';
  }

  /** Checks if the given version number is selected in dropdown A */
  isSelectedA(policyVersion: number): boolean {
    return this.selectedA()?.version.policyVersion === policyVersion;
  }

  /** Checks if the given version number is selected in dropdown B */
  isSelectedB(policyVersion: number): boolean {
    return this.selectedB()?.version.policyVersion === policyVersion;
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

  private readonly statusMap: Record<number, string> = {
    1: 'Draft',
    2: 'Published',
    3: 'Archived'
  };

  private normalizeStatus(status: unknown): string {
    if (status === null || status === undefined) return 'Draft';
    if (typeof status === 'number') {
      return this.statusMap[status] ?? 'Draft';
    }
    const text = String(status).toLowerCase();
    if (text === 'published') return 'Published';
    if (text === 'archived') return 'Archived';
    return 'Draft';
  }

  diffClass(key: string): string {
    return this.diffKeys().includes(key) ? 'diff' : '';
  }

  /** Returns a friendly label for a diff field key */
  fieldLabel(key: string): string {
    const field = this.fields.find(f => f.path === key);
    return field?.label ?? key;
  }

  /** Converts a policy version to a JsonObject for diffing */
  private versionToJsonObject(v: PolicyListItemDto['version']): JsonObject {
    return {
      status: v.status,
      baseXpAmount: v.baseXpAmount,
      currency: v.currency,
      claimWindowStartOffset: v.claimWindowStartOffset,
      claimWindowDuration: v.claimWindowDuration,
      anchorStrategy: v.anchorStrategy,
      graceAllowedMisses: v.graceAllowedMisses,
      graceWindowDays: v.graceWindowDays,
      streakModelType: v.streakModelType,
      streakModelParameters: v.streakModelParameters as JsonObject,
      previewSampleWindowDays: v.previewSampleWindowDays,
      previewDefaultSegment: v.previewDefaultSegment,
      effectiveAt: v.effectiveAt,
      supersededAt: v.supersededAt,
      createdAt: v.createdAt,
      createdBy: v.createdBy,
      publishedAt: v.publishedAt
    } as JsonObject;
  }

  private computeDiffs(): void {
    const a = this.selectedA();
    const b = this.selectedB();
    if (!a || !b) {
      this.diffs.set(null);
      this.diffKeys.set([]);
      return;
    }

    const left = this.versionToJsonObject(a.version);
    const right = this.versionToJsonObject(b.version);

    const diff = ObjectUtils.getDiffs(left, right);
    this.diffs.set(diff);
    const keys = new Set<string>([...Object.keys(diff.left), ...Object.keys(diff.right)]);
    this.diffKeys.set(Array.from(keys).sort());
  }

  protected formatDiffValue(key: string, side: 'left' | 'right'): string {
    const diff = this.diffs();
    if (!diff) return '—';
    const value = (diff as any)[side]?.[key];
    if (value === null || value === undefined || value === '') return '—';
    if (typeof value === 'object') {
      return ObjectUtils.stableStringify(value);
    }
    return value.toString();
  }
}
