import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { PolicyApiService, CreatePolicyVersionRequestDto, StreakCurveEntryDto } from '../../../core/api/policy-api.service';
import { AnchorStrategy } from '../../../core/api/policy-types';
import { ApiState, createInitialState } from '../../../core/utils/http';
import { StreakCurveEditorComponent } from '../streak-curve-editor/streak-curve-editor.component';

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
  previewDefaultSegment: FormControl<string>;
};

@Component({
  selector: 'app-policy-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StreakCurveEditorComponent],
  templateUrl: './policy-editor.component.html',
  styleUrls: ['./policy-editor.component.scss']
})
export class PolicyEditorComponent {
  readonly form: FormGroup<PolicyEditorForm>;

  protected readonly submitting = signal(false);
  protected readonly state = signal<ApiState<unknown>>(createInitialState());
  protected readonly streakCurve = signal<StreakCurveEntryDto[]>([
    { dayIndex: 0, multiplier: 1.0, additiveBonusXp: 0, capNextDay: false }
  ]);

  readonly anchorOptions: { label: string; value: AnchorStrategy }[] = [
    { label: 'Anchor Timezone', value: 'ANCHOR_TIMEZONE' },
    { label: 'Fixed UTC', value: 'FIXED_UTC' },
    { label: 'Server Local', value: 'SERVER_LOCAL' }
  ];

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: PolicyApiService
  ) {
    this.form = fb.nonNullable.group<PolicyEditorForm>({
      policyKey: fb.nonNullable.control('', [Validators.required, Validators.minLength(3)]),
      displayName: fb.nonNullable.control('', [Validators.required, Validators.maxLength(128)]),
      description: fb.nonNullable.control('', [Validators.maxLength(1024)]),
      baseXpAmount: fb.nonNullable.control(100, [Validators.required, Validators.min(1)]),
      currency: fb.nonNullable.control('XP', [Validators.required]),
      claimWindowStartMinutes: fb.nonNullable.control(0, [Validators.min(0), Validators.max(1439)]),
      claimWindowDurationHours: fb.nonNullable.control(24, [Validators.min(1), Validators.max(24)]),
      anchorStrategy: fb.nonNullable.control<AnchorStrategy>('ANCHOR_TIMEZONE', [Validators.required]),
      graceAllowedMisses: fb.nonNullable.control(0, [Validators.min(0)]),
      graceWindowDays: fb.nonNullable.control(7, [Validators.min(1)]),
      previewSampleWindowDays: fb.nonNullable.control(7, [Validators.min(1)]),
      previewDefaultSegment: fb.nonNullable.control('')
    });
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.state.set(createInitialState());

    try {
      const { policyKey, payload } = this.buildPayload();
      const result = await this.api.createPolicyVersion(policyKey, payload);

      this.state.set({
        loading: false,
        status: result.status,
        success: result.ok,
        body: result.body,
        error: result.ok ? null : result.error ?? 'Request failed'
      });
    } catch (error) {
      this.state.set({
        loading: false,
        status: null,
        success: false,
        body: null,
        error: error instanceof Error ? error.message : 'Unknown error'
      });
    } finally {
      this.submitting.set(false);
    }
  }

  private buildPayload(): { policyKey: string; payload: CreatePolicyVersionRequestDto } {
    const value = this.form.getRawValue();
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
        streakModelType: 'PlateauCap',
        streakModelParameters: { plateauDay: 1, plateauMultiplier: 1.0 },
        previewSampleWindowDays: value.previewSampleWindowDays,
        previewDefaultSegment: value.previewDefaultSegment || undefined,
        streakCurve: this.streakCurve(),
        seasonalBoosts: [],
        effectiveAt: undefined
      }
    };
  }

  onStreakChanged(entries: StreakCurveEntryDto[]): void {
    this.streakCurve.set(entries);
  }
}
