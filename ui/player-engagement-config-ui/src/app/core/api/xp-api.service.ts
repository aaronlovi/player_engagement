import { Inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../api-base-url.token';

export interface HealthResponse {
  status: string;
  error?: string;
}

export interface XpStubResponse {
  message?: string;
}

export interface ApiCallResult<T> {
  ok: boolean;
  status: number | null;
  body: T | null;
  error?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class XpApiService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  getHealthLive(): Promise<ApiCallResult<HealthResponse>> {
    return this.request<HealthResponse>('GET', '/health/live');
  }

  getHealthReady(): Promise<ApiCallResult<HealthResponse>> {
    return this.request<HealthResponse>('GET', '/health/ready');
  }

  getXpLedger(): Promise<ApiCallResult<XpStubResponse>> {
    return this.request<XpStubResponse>('GET', '/xp/ledger');
  }

  postXpGrant(): Promise<ApiCallResult<XpStubResponse>> {
    return this.request<XpStubResponse>('POST', '/xp/grants', {});
  }

  private async request<T>(method: string, path: string, body?: unknown): Promise<ApiCallResult<T>> {
    try {
      const response = await firstValueFrom(
        this.http.request<T>(method, `${this.baseUrl}${path}`, {
          body,
          observe: 'response'
        })
      );

      return {
        ok: true,
        status: response.status,
        body: (response.body ?? null) as T | null
      };
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        return {
          ok: false,
          status: error.status ?? null,
          body: (error.error ?? null) as T | null,
          error: this.normalizeErrorMessage(error)
        };
      }

      throw error;
    }
  }

  private normalizeErrorMessage(error: HttpErrorResponse): string {
    if (typeof error.error === 'string') {
      return error.error;
    }

    if (error.error && typeof error.error === 'object') {
      try {
        return JSON.stringify(error.error);
      } catch {
        // fall through to default
      }
    }

    return error.message;
  }
}
