import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StreakCurveEditorComponent } from './streak-curve-editor.component';

describe('StreakCurveEditorComponent', () => {
  let fixture: ComponentFixture<StreakCurveEditorComponent>;
  let component: StreakCurveEditorComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StreakCurveEditorComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(StreakCurveEditorComponent);
    component = fixture.componentInstance;
  });

  it('emits updated curve when adding a row to a valid sequence', () => {
    const emitted: any[] = [];
    component.changed.subscribe(value => emitted.push(value));

    component.entries = [
      { dayIndex: 0, multiplier: 1.0, additiveBonusXp: 0, capNextDay: false },
      { dayIndex: 1, multiplier: 1.2, additiveBonusXp: 0, capNextDay: false }
    ];

    component.addRow();

    expect(emitted.length).toBe(1);
    expect(emitted[0].length).toBe(3);
    expect(emitted[0][2].dayIndex).toBe(2);
    expect((component as any).hasError()).toBeFalse();
    expect((component as any).errorMessage()).toBeNull();
  });

  it('flags validation errors and does not emit when days are non-sequential', () => {
    const emitted: any[] = [];
    component.changed.subscribe(value => emitted.push(value));

    component.entries = [
      { dayIndex: 0, multiplier: 1.0, additiveBonusXp: 0, capNextDay: false },
      { dayIndex: 2, multiplier: 1.1, additiveBonusXp: 0, capNextDay: false }
    ];

    component.onFieldChange();

    expect(emitted.length).toBe(0);
    expect((component as any).hasError()).toBeTrue();
    expect((component as any).errorMessage()).toContain('Streak days must start at 0');
  });
});
