import { CommonModule } from '@angular/common';
import { Component, OnDestroy, signal } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';

import { PolicyApiService, CreatePolicyVersionRequestDto, SeasonalBoostDto, StreakCurveEntryDto } from '../../../core/api/policy-api.service';
import {
  AnchorStrategy,
  DecayCurveParameters,
  MilestoneMetaRewardParameters,
  MilestoneParameters,
  PlateauCapParameters,
  StreakModelType,
  TierParameters,
  TieredSeasonalResetParameters
} from '../../../core/api/policy-types';
import { ApiState, createInitialState, toApiState } from '../../../core/utils/http';
import { StreakCurveEditorComponent } from '../streak-curve-editor/streak-curve-editor.component';
import { SeasonalBoostsEditorComponent } from '../seasonal-boosts-editor/seasonal-boosts-editor.component';

type PolicyEditorForm = {
  policyKey: FormControl<string>;
  displayName: FormControl<string>;
  description: FormControl<string>;
  baseXpAmount: FormControl<number>;
  currency: FormControl<string>;
  claimWindowStartMinutes: FormControl<number>;
  claimWindowDurationHours: FormControl<number>;
  anchorStrategy: FormControl<AnchorStrategy>;
  graceAllowedMisses: FormControl<number>;
  graceWindowDays: FormControl<number>;
  previewSampleWindowDays: FormControl<number>;
  streakModelType: FormControl<StreakModelType>;
  enablePreviewDefaultSegment: FormControl<boolean>;
  previewDefaultSegment: FormControl<string>;
};

type PlateauForm = {
  plateauDay: FormControl<number>;
  plateauMultiplier: FormControl<number>;
};

type DecayForm = {
  decayPercent: FormControl<number>;
  graceDay: FormControl<number>;
};

type TierForm = {
  startDay: FormControl<number>;
  endDay: FormControl<number>;
  bonusMultiplier: FormControl<number>;
};

type MilestoneForm = {
  day: FormControl<number>;
  rewardType: FormControl<string>;
  rewardValue: FormControl<string>;
};

@Component({
  selector: 'app-policy-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StreakCurveEditorComponent, SeasonalBoostsEditorComponent],
  templateUrl: './policy-editor.component.html',
  styleUrls: ['./policy-editor.component.scss']
})
export class PolicyEditorComponent implements OnDestroy {
  readonly form: FormGroup<PolicyEditorForm>;
  readonly plateauForm: FormGroup<PlateauForm>;
  readonly decayForm: FormGroup<DecayForm>;
  readonly tiers: FormArray<FormGroup<TierForm>>;
  readonly milestones: FormArray<FormGroup<MilestoneForm>>;

  protected readonly submitting = signal(false);
  protected readonly state = signal<ApiState<unknown>>(createInitialState());
  protected readonly validationMessages = signal<string[]>([]);
  protected readonly toast = signal<{ kind: 'success' | 'error'; message: string } | null>(null);
  protected readonly validationError = signal<string | null>(null);
  protected readonly streakCurve = signal<StreakCurveEntryDto[]>([
    { dayIndex: 0, multiplier: 1.0, additiveBonusXp: 0, capNextDay: false }
  ]);
  protected readonly seasonalBoosts = signal<SeasonalBoostDto[]>([]);

  readonly anchorOptions: { label: string; value: AnchorStrategy }[] = [
    { label: 'Anchor Timezone', value: 'AnchorTimezone' },
    { label: 'Fixed UTC', value: 'FixedUtc' },
    { label: 'Server Local', value: 'ServerLocal' }
  ];

  readonly streakModelOptions: { label: string; value: StreakModelType; description: string }[] = [
    { label: 'Plateau cap', value: 'PlateauCap', description: 'Growth caps after a target day.' },
    { label: 'Weekly cycle reset', value: 'WeeklyCycleReset', description: 'Resets progress every 7 days.' },
    { label: 'Decay curve', value: 'DecayCurve', description: 'Soft reset after a grace period.' },
    { label: 'Tiered seasonal reset', value: 'TieredSeasonalReset', description: 'Tier-based bonuses within a season.' },
    { label: 'Milestone meta reward', value: 'MilestoneMetaReward', description: 'Extra rewards at configured milestones.' }
  ];

  private previewSegmentSub?: Subscription;

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: PolicyApiService
  ) {
    this.form = fb.nonNullable.group<PolicyEditorForm>({
      policyKey: fb.nonNullable.control('', [Validators.required, Validators.minLength(3)]),
      displayName: fb.nonNullable.control('', [Validators.required, Validators.maxLength(128)]),
      description: fb.nonNullable.control('', [Validators.maxLength(1024)]),
      baseXpAmount: fb.nonNullable.control(100, [Validators.required, Validators.min(1)]),
      currency: fb.nonNullable.control('XPX', [Validators.required, Validators.pattern(/^[A-Z]{3,8}$/)]),
      claimWindowStartMinutes: fb.nonNullable.control(0, [Validators.min(0), Validators.max(1439)]),
      claimWindowDurationHours: fb.nonNullable.control(24, [Validators.min(1), Validators.max(24)]),
      anchorStrategy: fb.nonNullable.control<AnchorStrategy>('AnchorTimezone', [Validators.required]),
      graceAllowedMisses: fb.nonNullable.control(0, [Validators.min(0)]),
      graceWindowDays: fb.nonNullable.control(7, [Validators.min(1)]),
      previewSampleWindowDays: fb.nonNullable.control(7, [Validators.min(1)]),
      streakModelType: fb.nonNullable.control<StreakModelType>('PlateauCap', [Validators.required]),
      enablePreviewDefaultSegment: fb.nonNullable.control(false),
      previewDefaultSegment: fb.nonNullable.control({ value: '', disabled: true }, [
        Validators.pattern(/^[A-Za-z0-9_]{1,32}$/)
      ])
    });

    this.plateauForm = fb.nonNullable.group<PlateauForm>({
      plateauDay: fb.nonNullable.control(1, [Validators.required, Validators.min(1)]),
      plateauMultiplier: fb.nonNullable.control(1.0, [Validators.required, Validators.min(0.01)])
    });

    this.decayForm = fb.nonNullable.group<DecayForm>({
      decayPercent: fb.nonNullable.control(0.1, [Validators.required, Validators.min(0), Validators.max(1)]),
      graceDay: fb.nonNullable.control(0, [Validators.required, Validators.min(0)])
    });

    this.tiers = fb.nonNullable.array<FormGroup<TierForm>>([this.createTierGroup()]);
    this.milestones = fb.nonNullable.array<FormGroup<MilestoneForm>>([this.createMilestoneGroup()]);

    this.previewSegmentSub = this.form.controls.enablePreviewDefaultSegment.valueChanges.subscribe(enabled => {
      const control = this.form.controls.previewDefaultSegment;
      if (enabled) {
        control.setValidators([Validators.required, Validators.pattern(/^[A-Za-z0-9_]{1,32}$/)]);
        control.enable({ emitEvent: false });
      } else {
        control.clearValidators();
        control.setValue('', { emitEvent: false });
        control.disable({ emitEvent: false });
      }

      control.updateValueAndValidity({ emitEvent: false });
    });
  }

  async submit(): Promise<void> {
    this.validationError.set(null);
    this.validationMessages.set([]);
    this.toast.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const modelType = this.form.controls.streakModelType.value;
    if (!this.ensureParameterFormsValid(modelType)) {
      return;
    }

    this.submitting.set(true);
    this.state.set(createInitialState());

    try {
      const { policyKey, payload } = this.buildPayload();
      const result = await this.api.createPolicyVersion(policyKey, payload);

      this.state.set(toApiState(result));
      if (result.ok) {
        this.toast.set({ kind: 'success', message: 'Draft created successfully.' });
      } else {
        this.validationMessages.set(this.extractValidationMessages(result.body));
        this.toast.set({ kind: 'error', message: result.error ?? 'Request failed' });
      }
    } catch (error) {
      this.state.set({
        loading: false,
        status: null,
        success: false,
        body: null,
        error: error instanceof Error ? error.message : 'Unknown error'
      });
      this.toast.set({ kind: 'error', message: error instanceof Error ? error.message : 'Unknown error' });
    } finally {
      this.submitting.set(false);
    }
  }

  private buildPayload(): { policyKey: string; payload: CreatePolicyVersionRequestDto } {
    const value = this.form.getRawValue();
    const streakModelType = value.streakModelType;
    const previewDefaultSegment = value.enablePreviewDefaultSegment
      ? value.previewDefaultSegment.trim() || undefined
      : undefined;

    return {
      policyKey: value.policyKey,
      payload: {
        displayName: value.displayName,
        description: value.description || undefined,
        baseXpAmount: value.baseXpAmount,
        currency: value.currency,
        claimWindowStartMinutes: value.claimWindowStartMinutes,
        claimWindowDurationHours: value.claimWindowDurationHours,
        anchorStrategy: value.anchorStrategy,
        graceAllowedMisses: value.graceAllowedMisses,
        graceWindowDays: Math.max(value.graceWindowDays, value.graceAllowedMisses),
        streakModelType,
        streakModelParameters: this.buildStreakModelParameters(streakModelType),
        previewSampleWindowDays: value.previewSampleWindowDays,
        previewDefaultSegment,
        streakCurve: this.streakCurve(),
        seasonalBoosts: this.seasonalBoosts(),
        effectiveAt: undefined
      }
    };
  }

  private buildStreakModelParameters(type: StreakModelType): PlateauCapParameters | DecayCurveParameters | TieredSeasonalResetParameters | MilestoneMetaRewardParameters | Record<string, never> {
    switch (type) {
      case 'PlateauCap':
        return {
          plateauDay: Math.max(1, Math.trunc(this.plateauForm.controls.plateauDay.value)),
          plateauMultiplier: this.plateauForm.controls.plateauMultiplier.value
        };
      case 'WeeklyCycleReset':
        return {};
      case 'DecayCurve':
        return {
          decayPercent: this.decayForm.controls.decayPercent.value,
          graceDay: Math.max(0, Math.trunc(this.decayForm.controls.graceDay.value))
        };
      case 'TieredSeasonalReset':
        return {
          tiers: this.tiers.controls.map(group => this.mapTier(group))
        };
      case 'MilestoneMetaReward':
        return {
          milestones: this.milestones.controls.map(group => this.mapMilestone(group))
        };
      default:
        return {};
    }
  }

  private ensureParameterFormsValid(type: StreakModelType): boolean {
    switch (type) {
      case 'PlateauCap':
        this.plateauForm.markAllAsTouched();
        return this.plateauForm.valid;
      case 'WeeklyCycleReset':
        return true;
      case 'DecayCurve':
        this.decayForm.markAllAsTouched();
        return this.decayForm.valid;
      case 'TieredSeasonalReset':
        this.tiers.markAllAsTouched();
        if (this.tiers.length === 0) {
          this.validationError.set('Add at least one tier for tiered seasonal reset.');
          return false;
        }

        if (this.tiers.controls.some(group => group.controls.endDay.value < group.controls.startDay.value)) {
          this.validationError.set('Tier end day must be greater than or equal to start day.');
          return false;
        }

        return this.tiers.controls.every(group => group.valid);
      case 'MilestoneMetaReward':
        this.milestones.markAllAsTouched();
        if (this.milestones.length === 0) {
          this.validationError.set('Add at least one milestone.');
          return false;
        }

        return this.milestones.controls.every(group => group.valid);
      default:
        return false;
    }
  }

  addTier(): void {
    this.tiers.push(this.createTierGroup());
  }

  removeTier(index: number): void {
    if (this.tiers.length === 1) {
      this.tiers.at(0).reset({
        startDay: 1,
        endDay: 1,
        bonusMultiplier: 1.0
      });
      return;
    }

    this.tiers.removeAt(index);
  }

  addMilestone(): void {
    this.milestones.push(this.createMilestoneGroup());
  }

  removeMilestone(index: number): void {
    if (this.milestones.length === 1) {
      this.milestones.at(0).reset({
        day: 1,
        rewardType: '',
        rewardValue: ''
      });
      return;
    }

    this.milestones.removeAt(index);
  }

  private createTierGroup(): FormGroup<TierForm> {
    return this.fb.nonNullable.group<TierForm>({
      startDay: this.fb.nonNullable.control(1, [Validators.required, Validators.min(1)]),
      endDay: this.fb.nonNullable.control(1, [Validators.required, Validators.min(1)]),
      bonusMultiplier: this.fb.nonNullable.control(1.0, [Validators.required, Validators.min(0.01)])
    });
  }

  private createMilestoneGroup(): FormGroup<MilestoneForm> {
    return this.fb.nonNullable.group<MilestoneForm>({
      day: this.fb.nonNullable.control(1, [Validators.required, Validators.min(1)]),
      rewardType: this.fb.nonNullable.control('', [Validators.required]),
      rewardValue: this.fb.nonNullable.control('', [Validators.required])
    });
  }

  private mapTier(group: FormGroup<TierForm>): TierParameters {
    return {
      startDay: Math.max(1, Math.trunc(group.controls.startDay.value)),
      endDay: Math.max(1, Math.trunc(group.controls.endDay.value)),
      bonusMultiplier: group.controls.bonusMultiplier.value
    };
  }

  private mapMilestone(group: FormGroup<MilestoneForm>): MilestoneParameters {
    return {
      day: Math.max(1, Math.trunc(group.controls.day.value)),
      rewardType: group.controls.rewardType.value.trim(),
      rewardValue: group.controls.rewardValue.value.trim()
    };
  }

  onStreakChanged(entries: StreakCurveEntryDto[]): void {
    this.streakCurve.set(entries);
  }

  onSeasonalBoostsChanged(entries: SeasonalBoostDto[]): void {
    this.seasonalBoosts.set(entries);
  }

  describeStreakModel(): string {
    const selected = this.form.controls.streakModelType.value;
    const option = this.streakModelOptions.find(o => o.value === selected);
    return option?.description ?? 'Select a streak model';
  }

  private extractValidationMessages(body: unknown): string[] {
    if (!body || typeof body !== 'object') {
      return [];
    }

    const messages: string[] = [];
    const candidate = body as { errors?: Record<string, unknown>; title?: unknown; detail?: unknown; message?: unknown };

    if (candidate.errors && typeof candidate.errors === 'object') {
      for (const [field, value] of Object.entries(candidate.errors)) {
        if (Array.isArray(value)) {
          messages.push(`${field}: ${value.join(', ')}`);
        } else if (typeof value === 'string') {
          messages.push(`${field}: ${value}`);
        }
      }
    }

    const extras = [candidate.title, candidate.detail, candidate.message].filter(v => typeof v === 'string') as string[];
    for (const extra of extras) {
      if (messages.includes(extra)) continue;
      messages.push(extra);
    }

    return messages;
  }

  showPreviewDefaultSegment(): boolean {
    return this.form.controls.enablePreviewDefaultSegment.value;
  }

  previewSegmentInvalid(): boolean {
    return this.isControlInvalid(this.form.controls.previewDefaultSegment);
  }

  hasStatus(): boolean {
    return this.state().status !== null;
  }

  responseStatus(): number | null {
    return this.state().status;
  }

  hasError(): boolean {
    return !!this.state().error;
  }

  errorMessage(): string | null {
    return this.state().error;
  }

  hasValidationMessages(): boolean {
    return this.validationMessages().length > 0;
  }

  validationMessagesList(): string[] {
    return this.validationMessages();
  }

  currentToast() {
    return this.toast();
  }

  isControlInvalid(control: AbstractControl | null | undefined): boolean {
    return !!control && control.invalid && control.touched;
  }

  ngOnDestroy(): void {
    this.previewSegmentSub?.unsubscribe();
  }
}
