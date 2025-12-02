import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { PolicyApiService } from '../../../core/api/policy-api.service';
import { PolicyOverridesComponent } from './policy-overrides.component';

describe('PolicyOverridesComponent', () => {
  let fixture: ComponentFixture<PolicyOverridesComponent>;
  let component: PolicyOverridesComponent;
  let apiSpy: jasmine.SpyObj<PolicyApiService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj<PolicyApiService>('PolicyApiService', ['getSegmentOverrides', 'updateSegmentOverrides']);

    await TestBed.configureTestingModule({
      imports: [PolicyOverridesComponent],
      providers: [
        { provide: PolicyApiService, useValue: apiSpy },
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ policyKey: 'policy-123' })) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PolicyOverridesComponent);
    component = fixture.componentInstance;
  });

  it('loads overrides and maps them into form rows', async () => {
    apiSpy.getSegmentOverrides.and.resolveTo({
      ok: true,
      status: 200,
      body: { alpha: 2, beta: 3 }
    });

    component.form.controls.policyKey.setValue('policy-123');
    await component.load();

    expect(apiSpy.getSegmentOverrides).toHaveBeenCalledWith('policy-123');
    expect(component.overrideRows.length).toBe(2);
    expect(component.overrideRows.at(0).getRawValue()).toEqual({ segmentKey: 'alpha', policyVersion: 2 });
    expect(component.overrideRows.at(1).getRawValue()).toEqual({ segmentKey: 'beta', policyVersion: 3 });
    expect((component as any).state().success).toBeTrue();
  });

  it('saves overrides using the composed form values', async () => {
    apiSpy.updateSegmentOverrides.and.resolveTo({ ok: true, status: 200, body: { } as Record<string, number> });

    component.form.controls.policyKey.setValue('policy-123');
    component.addRow();
    component.overrideRows.at(0).setValue({ segmentKey: 'SegmentA', policyVersion: 1 });
    component.addRow();
    component.overrideRows.at(1).setValue({ segmentKey: 'SegmentB', policyVersion: 2 });

    await component.save();

    expect(apiSpy.updateSegmentOverrides).toHaveBeenCalledWith('policy-123', {
      SegmentA: 1,
      SegmentB: 2
    });
    expect((component as any).state().success).toBeTrue();
    expect((component as any).message()).toContain('Overrides updated');
  });
});
