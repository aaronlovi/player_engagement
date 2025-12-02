import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import { StreakCurveEntryDto } from '../../../core/api/policy-api.service';

type StreakForm = FormGroup<{
  dayIndex: FormControl<number>;
  multiplier: FormControl<number>;
  additiveBonusXp: FormControl<number>;
  capNextDay: FormControl<boolean>;
}>;

@Component({
  selector: 'app-streak-curve-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './streak-curve-editor.component.html',
  styleUrls: ['./streak-curve-editor.component.scss']
})
export class StreakCurveEditorComponent {
  @Input() set entries(value: StreakCurveEntryDto[] | null) {
    const safe = value ?? [];
    this.rows = safe.map((e) => this.createRow(e));
    this.emitChanges();
  }

  @Output() changed = new EventEmitter<StreakCurveEntryDto[]>();

  protected rows: StreakForm[] = [];
  protected readonly hasError = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor(private readonly fb: FormBuilder) {}

  addRow(): void {
    const nextDay = this.rows.length > 0 ? (this.rows[this.rows.length - 1].getRawValue().dayIndex ?? 0) + 1 : 0;
    this.rows.push(
      this.createRow({
        dayIndex: nextDay,
        multiplier: 1.0,
        additiveBonusXp: 0,
        capNextDay: false
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

  private createRow(entry: StreakCurveEntryDto): StreakForm {
    return this.fb.nonNullable.group({
      dayIndex: this.fb.nonNullable.control(entry.dayIndex, [Validators.required, Validators.min(0)]),
      multiplier: this.fb.nonNullable.control(entry.multiplier, [Validators.required, Validators.min(0)]),
      additiveBonusXp: this.fb.nonNullable.control(entry.additiveBonusXp, [Validators.min(0)]),
      capNextDay: this.fb.nonNullable.control(entry.capNextDay)
    });
  }

  private emitChanges(): void {
    const dto: StreakCurveEntryDto[] = this.rows.map((row) => {
      const value = row.getRawValue();
      return {
        dayIndex: value.dayIndex,
        multiplier: value.multiplier,
        additiveBonusXp: value.additiveBonusXp,
        capNextDay: value.capNextDay
      };
    });

    const { valid, message } = this.validateCurve(dto);
    this.hasError.set(!valid);
    this.errorMessage.set(message);

    if (valid) {
      this.changed.emit(dto);
    }
  }

  private validateCurve(entries: StreakCurveEntryDto[]): { valid: boolean; message: string | null } {
    if (entries.length === 0) return { valid: true, message: null };

    // Ensure dayIndex is ascending and starts at 0
    const sorted = [...entries].sort((a, b) => a.dayIndex - b.dayIndex);
    for (let i = 0; i < sorted.length; i++) {
      if (sorted[i].dayIndex !== i) {
        return { valid: false, message: 'Streak days must start at 0 and increment by 1 without gaps.' };
      }
    }

    // Basic bounds checks
    for (const entry of entries) {
      if (entry.multiplier < 0) return { valid: false, message: 'Multiplier cannot be negative.' };
      if (entry.additiveBonusXp < 0) return { valid: false, message: 'Bonus XP cannot be negative.' };
    }

    return { valid: true, message: null };
  }
}
