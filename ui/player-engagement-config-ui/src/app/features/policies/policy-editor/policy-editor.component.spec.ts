import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CreatePolicyVersionRequestDto, PolicyApiService } from '../../../core/api/policy-api.service';
import { PolicyEditorComponent } from './policy-editor.component';

describe('PolicyEditorComponent', () => {
  let fixture: ComponentFixture<PolicyEditorComponent>;
  let component: PolicyEditorComponent;
  let apiSpy: jasmine.SpyObj<PolicyApiService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj<PolicyApiService>('PolicyApiService', ['createPolicyVersion']);
    apiSpy.createPolicyVersion.and.resolveTo({ ok: true, status: 200, body: null });

    await TestBed.configureTestingModule({
      imports: [PolicyEditorComponent],
      providers: [{ provide: PolicyApiService, useValue: apiSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(PolicyEditorComponent);
    component = fixture.componentInstance;
  });

  it('builds a normalized payload when submitting a plateau policy', async () => {
    component.form.setValue({
      policyKey: 'winter_rewards',
      displayName: 'Winter Rewards',
      description: 'Double XP weekends',
      baseXpAmount: 500,
      currency: 'XPX',
      claimWindowStartMinutes: 5,
      claimWindowDurationHours: 12,
      anchorStrategy: 'AnchorTimezone',
      graceAllowedMisses: 5,
      graceWindowDays: 3,
      previewSampleWindowDays: 14,
      streakModelType: 'PlateauCap',
      enablePreviewDefaultSegment: true,
      previewDefaultSegment: 'vip_team'
    });
    component.plateauForm.controls.plateauDay.setValue(4.7);
    component.plateauForm.controls.plateauMultiplier.setValue(2.5);
    component.onStreakChanged([
      { dayIndex: 0, multiplier: 1.5, additiveBonusXp: 10, capNextDay: false }
    ]);
    component.onSeasonalBoostsChanged([
      { label: 'Spring boost', multiplier: 1.1, startUtc: '2024-01-01T00:00:00.000Z', endUtc: '2024-01-02T00:00:00.000Z' }
    ]);

    await component.submit();

    expect(apiSpy.createPolicyVersion).toHaveBeenCalled();
    const [policyKey, payload] = apiSpy.createPolicyVersion.calls.mostRecent().args as [string, CreatePolicyVersionRequestDto];
    expect(policyKey).toBe('winter_rewards');
    expect(payload.graceWindowDays).toBe(5);
    expect(payload.previewDefaultSegment).toBe('vip_team');
    expect(payload.streakModelParameters).toEqual({ plateauDay: 4, plateauMultiplier: 2.5 });
    expect(payload.streakCurve[0]).toEqual({ dayIndex: 0, multiplier: 1.5, additiveBonusXp: 10, capNextDay: false });
    expect(payload.seasonalBoosts[0]).toEqual(jasmine.objectContaining({
      label: 'Spring boost',
      multiplier: 1.1,
      startUtc: '2024-01-01T00:00:00.000Z',
      endUtc: '2024-01-02T00:00:00.000Z'
    }));
  });

  it('blocks submission when tier configuration is invalid', async () => {
    component.form.setValue({
      policyKey: 'tiered_policy',
      displayName: 'Tiered',
      description: '',
      baseXpAmount: 100,
      currency: 'XPX',
      claimWindowStartMinutes: 0,
      claimWindowDurationHours: 24,
      anchorStrategy: 'AnchorTimezone',
      graceAllowedMisses: 0,
      graceWindowDays: 1,
      previewSampleWindowDays: 7,
      streakModelType: 'TieredSeasonalReset',
      enablePreviewDefaultSegment: false,
      previewDefaultSegment: ''
    });
    component.tiers.at(0).setValue({ startDay: 5, endDay: 3, bonusMultiplier: 1.5 });

    await component.submit();

    expect(apiSpy.createPolicyVersion).not.toHaveBeenCalled();
    expect((component as any).validationError()).toContain('Tier end day must be greater than or equal to start day.');
  });
});
