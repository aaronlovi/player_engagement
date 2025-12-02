import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';

import { PolicyApiService } from '../../../core/api/policy-api.service';
import { ApiState, createInitialState, toApiState } from '../../../core/utils/http';

type OverrideRow = {
  segmentKey: FormControl<string>;
  policyVersion: FormControl<number>;
};

type OverridesForm = {
  policyKey: FormControl<string>;
  overrides: FormArray<FormGroup<OverrideRow>>;
};

@Component({
  selector: 'app-policy-overrides',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './policy-overrides.component.html',
  styleUrls: ['./policy-overrides.component.scss']
})
export class PolicyOverridesComponent implements OnInit, OnDestroy {
  readonly form: FormGroup<OverridesForm>;

  protected readonly state = signal<ApiState<unknown>>(createInitialState());
  protected readonly submitting = signal(false);
  protected readonly message = signal<string | null>(null);

  private paramSub?: Subscription;

  constructor(
    private readonly fb: FormBuilder,
    private readonly api: PolicyApiService,
    private readonly route: ActivatedRoute
  ) {
    this.form = fb.nonNullable.group<OverridesForm>({
      policyKey: fb.nonNullable.control('', [Validators.required, Validators.minLength(3)]),
      overrides: fb.nonNullable.array<FormGroup<OverrideRow>>([])
    });
  }

  ngOnInit(): void {
    this.paramSub = this.route.paramMap.subscribe(params => {
      const key = params.get('policyKey');
      if (key) {
        this.form.controls.policyKey.setValue(key);
        void this.load();
      }
    });
  }

  ngOnDestroy(): void {
    this.paramSub?.unsubscribe();
  }

  get overrideRows(): FormArray<FormGroup<OverrideRow>> {
    return this.form.controls.overrides;
  }

  async load(): Promise<void> {
    if (this.form.controls.policyKey.invalid) {
      this.form.controls.policyKey.markAsTouched();
      return;
    }

    this.state.set({ ...createInitialState(), loading: true });
    this.message.set(null);
    this.overrideRows.clear();

    const policyKey = this.form.controls.policyKey.value;
    const result = await this.api.getSegmentOverrides(policyKey);
    this.state.set(toApiState(result));

    if (!result.ok || !result.body) {
      this.ensureRow();
      return;
    }

    const entries = Object.entries(result.body);
    if (entries.length === 0) {
      this.ensureRow();
      return;
    }

    for (const [segment, version] of entries) {
      this.overrideRows.push(this.createRow(segment, version));
    }
  }

  addRow(): void {
    this.overrideRows.push(this.createRow());
  }

  removeRow(index: number): void {
    if (this.overrideRows.length === 1) {
      this.overrideRows.at(0).reset({
        segmentKey: '',
        policyVersion: 0
      });
      return;
    }

    this.overrideRows.removeAt(index);
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.state.set({ ...createInitialState(), loading: true });
    this.message.set(null);

    const key = this.form.controls.policyKey.value;
    const overrides: Record<string, number> = {};
    for (const row of this.overrideRows.controls) {
      const segment = row.controls.segmentKey.value.trim();
      const version = row.controls.policyVersion.value;
      if (!segment) continue;
      overrides[segment] = version;
    }

    const result = await this.api.updateSegmentOverrides(key, overrides);
    this.state.set(toApiState(result));
    if (result.ok) {
      this.message.set('Overrides updated.');
    }

    this.submitting.set(false);
  }

  isInvalid(control: FormControl | null | undefined): boolean {
    return !!control && control.invalid && control.touched;
  }

  private createRow(segmentKey = '', policyVersion = 0): FormGroup<OverrideRow> {
    return this.fb.nonNullable.group<OverrideRow>({
      segmentKey: this.fb.nonNullable.control(segmentKey, [
        Validators.required,
        Validators.pattern(/^[A-Za-z0-9_]{1,32}$/)
      ]),
      policyVersion: this.fb.nonNullable.control(policyVersion, [Validators.required, Validators.min(1)])
    });
  }

  private ensureRow(): void {
    if (this.overrideRows.length === 0) {
      this.overrideRows.push(this.createRow());
    }
  }
}
