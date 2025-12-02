import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { PolicyHistoryComponent } from './policy-history.component';
import { PolicyApiService } from '../../../core/api/policy-api.service';

describe('PolicyHistoryComponent', () => {
  let fixture: ComponentFixture<PolicyHistoryComponent>;
  let component: PolicyHistoryComponent;

  beforeEach(async () => {
    const apiSpy = jasmine.createSpyObj<PolicyApiService>('PolicyApiService', ['listPolicyVersions']);
    apiSpy.listPolicyVersions.and.resolveTo({ ok: true, status: 200, body: [] });

    await TestBed.configureTestingModule({
      imports: [PolicyHistoryComponent],
      providers: [
        { provide: PolicyApiService, useValue: apiSpy },
        provideRouter([])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PolicyHistoryComponent);
    component = fixture.componentInstance;
  });

  it('creates', () => {
    expect(component).toBeTruthy();
  });
});
