import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal, WritableSignal } from '@angular/core';

import { ApiCallResult, HealthResponse, XpApiService, XpStubResponse } from '../../core/api/xp-api.service';
import { ApiState, createInitialState } from '../../core/utils/http';

@Component({
  selector: 'app-health-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './health-page.component.html'
})
export class HealthPageComponent implements OnInit {
  protected readonly liveState = signal<ApiState<HealthResponse>>(createInitialState<HealthResponse>());
  protected readonly readyState = signal<ApiState<HealthResponse>>(createInitialState<HealthResponse>());
  protected readonly ledgerState = signal<ApiState<XpStubResponse>>(createInitialState<XpStubResponse>());
  protected readonly grantState = signal<ApiState<XpStubResponse>>(createInitialState<XpStubResponse>());

  protected readonly isRefreshing = computed(
    () => this.liveState().loading || this.readyState().loading || this.ledgerState().loading
  );

  protected readonly isGrantPending = computed(() => this.grantState().loading);

  constructor(private readonly api: XpApiService) {}

  ngOnInit(): void {
    void this.refreshAll();
  }

  async refreshAll(): Promise<void> {
    await Promise.all([
      this.loadState(this.liveState, () => this.api.getHealthLive()),
      this.loadState(this.readyState, () => this.api.getHealthReady()),
      this.loadState(this.ledgerState, () => this.api.getXpLedger())
    ]);
  }

  async callXpGrant(): Promise<void> {
    await this.loadState(this.grantState, () => this.api.postXpGrant());
  }

  protected toJson(body: unknown): string {
    if (body == null) return '—';
    if (typeof body === 'string') return body;
    try {
      return JSON.stringify(body, null, 2);
    } catch {
      return String(body);
    }
  }

  private async loadState<T>(
    state: WritableSignal<ApiState<T>>,
    factory: () => Promise<ApiCallResult<T>>
  ): Promise<void> {
    state.set({ ...createInitialState<T>(), loading: true });

    try {
      const result = await factory();
      state.set({
        loading: false,
        status: result.status,
        success: result.ok,
        body: result.body,
        error: result.ok ? null : result.error ?? null
      });
    } catch (error) {
      state.set({
        loading: false,
        status: null,
        success: false,
        body: null,
        error: error instanceof Error ? error.message : 'Unknown error'
      });
    }
  }
}
