import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { SeasonalBoostDto } from '../../../core/api/policy-api.service';

type BoostForm = FormGroup<{
  label: FormControl<string>;
  multiplier: FormControl<number>;
  startUtc: FormControl<string>;
  endUtc: FormControl<string>;
}>;

@Component({
  selector: 'app-seasonal-boosts-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './seasonal-boosts-editor.component.html',
  styleUrls: ['./seasonal-boosts-editor.component.scss']
})
export class SeasonalBoostsEditorComponent {
  @Input() set boosts(value: SeasonalBoostDto[] | null) {
    const safe = value ?? [];
    this.rows = safe.map((b) => this.createRow(b));
    // Avoid emitting here to prevent parent→child→parent loops; emit only on user actions.
  }

  @Output() changed = new EventEmitter<SeasonalBoostDto[]>();

  protected rows: BoostForm[] = [];
  protected readonly error = signal<string | null>(null);

  constructor(private readonly fb: FormBuilder) {}

  addRow(): void {
    const now = new Date();
    const start = now.toISOString();
    const end = new Date(now.getTime() + 24 * 60 * 60 * 1000).toISOString();

    this.rows.push(
      this.createRow({
        label: 'New boost',
        multiplier: 1.1,
        startUtc: start,
        endUtc: end
      })
    );
    this.emitChanges();
  }

  removeRow(index: number): void {
    if (index < 0 || index >= this.rows.length) return;
    this.rows.splice(index, 1);
    this.emitChanges();
  }

  onFieldChange(): void {
    this.emitChanges();
  }

  private createRow(boost: SeasonalBoostDto): BoostForm {
    return this.fb.nonNullable.group({
      label: this.fb.nonNullable.control(boost.label, [Validators.required, Validators.maxLength(64)]),
      multiplier: this.fb.nonNullable.control(boost.multiplier, [Validators.required, Validators.min(1)]),
      startUtc: this.fb.nonNullable.control(boost.startUtc, [Validators.required]),
      endUtc: this.fb.nonNullable.control(boost.endUtc, [Validators.required])
    });
  }

  private emitChanges(): void {
    const dto: SeasonalBoostDto[] = this.rows.map((row, index) => {
      const value = row.getRawValue();
      return {
        boostId: 0,
        label: value.label,
        multiplier: value.multiplier,
        startUtc: value.startUtc,
        endUtc: value.endUtc
      };
    });

    const validation = this.validate(dto);
    this.error.set(validation);

    if (!validation) {
      this.changed.emit(dto);
    }
  }

  private validate(boosts: SeasonalBoostDto[]): string | null {
    if (boosts.length === 0) return null;

    // Basic window checks
    for (const boost of boosts) {
      const start = Date.parse(boost.startUtc);
      const end = Date.parse(boost.endUtc);
      if (isNaN(start) || isNaN(end)) return 'Invalid date in seasonal boosts.';
      if (end <= start) return 'End time must be after start time.';
      if (boost.multiplier < 1) return 'Multiplier must be >= 1.';
    }

    // Overlap detection
    const sorted = [...boosts].sort((a, b) => Date.parse(a.startUtc) - Date.parse(b.startUtc));
    for (let i = 1; i < sorted.length; i++) {
      const prevEnd = Date.parse(sorted[i - 1].endUtc);
      const currentStart = Date.parse(sorted[i].startUtc);
      if (currentStart < prevEnd) {
        return 'Seasonal boost windows cannot overlap.';
      }
    }

    return null;
  }
}
