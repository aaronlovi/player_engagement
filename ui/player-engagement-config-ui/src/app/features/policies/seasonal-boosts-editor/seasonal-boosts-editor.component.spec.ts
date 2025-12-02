import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SeasonalBoostsEditorComponent } from './seasonal-boosts-editor.component';

describe('SeasonalBoostsEditorComponent', () => {
  let fixture: ComponentFixture<SeasonalBoostsEditorComponent>;
  let component: SeasonalBoostsEditorComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SeasonalBoostsEditorComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(SeasonalBoostsEditorComponent);
    component = fixture.componentInstance;
  });

  it('emits boosts when input is valid', () => {
    const emitted: any[] = [];
    component.changed.subscribe(value => emitted.push(value));

    component.boosts = [
      {
        label: 'Winter launch',
        multiplier: 1.25,
        startUtc: '2024-01-01T00:00:00.000Z',
        endUtc: '2024-01-02T00:00:00.000Z'
      }
    ];

    component.onFieldChange();

    expect(emitted.length).toBe(1);
    expect(emitted[0][0]).toEqual(jasmine.objectContaining({
      label: 'Winter launch',
      multiplier: 1.25,
      startUtc: '2024-01-01T00:00:00.000Z',
      endUtc: '2024-01-02T00:00:00.000Z'
    }));
    expect((component as any).error()).toBeNull();
  });

  it('surfaces overlap errors and suppresses emits for invalid ranges', () => {
    const emitted: any[] = [];
    component.changed.subscribe(value => emitted.push(value));

    component.boosts = [
      {
        label: 'Overlap A',
        multiplier: 1.2,
        startUtc: '2024-01-01T00:00:00.000Z',
        endUtc: '2024-01-02T00:00:00.000Z'
      },
      {
        label: 'Overlap B',
        multiplier: 1.1,
        startUtc: '2024-01-01T12:00:00.000Z',
        endUtc: '2024-01-03T00:00:00.000Z'
      }
    ];

    component.onFieldChange();

    expect(emitted.length).toBe(0);
    expect((component as any).error()).toContain('overlap');
  });
});
