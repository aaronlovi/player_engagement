import { Inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../api-base-url.token';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../utils/http';
import { PolicyVersionStatus } from './policy-types';

export interface StreakCurveEntryDto {
  dayIndex: number;
  multiplier: number;
  additiveBonusXp: number;
  capNextDay: boolean;
}

export interface SeasonalBoostDto {
  boostId?: number;
  label: string;
  multiplier: number;
  startUtc: string;
  endUtc: string;
}

export interface PolicyVersionDto {
  policyVersion: number;
  status: PolicyVersionStatus;
  baseXpAmount: number;
  currency: string;
  claimWindowStartOffset: string;
  claimWindowDuration: string;
  anchorStrategy: string;
  graceAllowedMisses: number;
  graceWindowDays: number;
  streakModelType: string;
  streakModelParameters: Record<string, unknown>;
  previewSampleWindowDays: number;
  previewDefaultSegment?: string | null;
  effectiveAt?: string | null;
  supersededAt?: string | null;
  createdAt: string;
  createdBy: string;
  publishedAt?: string | null;
}

export interface PolicyDocumentDto {
  policyKey: string;
  displayName: string;
  description: string;
  version: PolicyVersionDto;
  streakCurve: StreakCurveEntryDto[];
  seasonalBoosts: SeasonalBoostDto[];
}

export interface PolicyListItemDto {
  policyKey: string;
  displayName: string;
  description: string;
  version: PolicyVersionDto;
}

export interface CreatePolicyVersionRequestDto {
  displayName: string;
  description?: string | null;
  baseXpAmount: number;
  currency: string;
  claimWindowStartMinutes: number;
  claimWindowDurationHours: number;
  anchorStrategy: string;
  graceAllowedMisses: number;
  graceWindowDays: number;
  streakModelType: string;
  streakModelParameters: Record<string, unknown>;
  previewSampleWindowDays: number;
  previewDefaultSegment?: string | null;
  streakCurve: StreakCurveEntryDto[];
  seasonalBoosts: SeasonalBoostDto[];
  effectiveAt?: string | null;
}

export interface PublishPolicyVersionRequestDto {
  effectiveAt?: string | null;
  segmentOverrides?: Record<string, number>;
}

export interface RetirePolicyVersionRequestDto {
  retiredAt?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class PolicyApiService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) { }

  listPolicyVersions(
    policyKey: string,
    options?: { status?: PolicyVersionStatus; effectiveBefore?: string; limit?: number }
  ): Promise<ApiResult<PolicyListItemDto[]>> {
    const params = this.buildParams({
      status: options?.status,
      effectiveBefore: options?.effectiveBefore,
      limit: options?.limit
    });
    return this.request<PolicyListItemDto[]>('GET', `/xp/policies/${policyKey}/versions`, { params });
  }

  getPolicyVersion(policyKey: string, policyVersion: number): Promise<ApiResult<PolicyDocumentDto>> {
    return this.request<PolicyDocumentDto>('GET', `/xp/policies/${policyKey}/versions/${policyVersion}`);
  }

  getActivePolicy(policyKey: string, segment?: string): Promise<ApiResult<PolicyDocumentDto>> {
    const params = this.buildParams({ policyKey, segment });
    return this.request<PolicyDocumentDto>('GET', `/xp/policies/active`, { params });
  }

  createPolicyVersion(policyKey: string, payload: CreatePolicyVersionRequestDto): Promise<ApiResult<PolicyDocumentDto>> {
    return this.request<PolicyDocumentDto>('POST', `/xp/policies/${policyKey}/versions`, { body: payload });
  }

  publishPolicyVersion(
    policyKey: string,
    policyVersion: number,
    payload: PublishPolicyVersionRequestDto
  ): Promise<ApiResult<PolicyDocumentDto>> {
    return this.request<PolicyDocumentDto>('POST', `/xp/policies/${policyKey}/versions/${policyVersion}/publish`, {
      body: payload
    });
  }

  retirePolicyVersion(
    policyKey: string,
    policyVersion: number,
    payload: RetirePolicyVersionRequestDto
  ): Promise<ApiResult<PolicyVersionDto>> {
    return this.request<PolicyVersionDto>('POST', `/xp/policies/${policyKey}/versions/${policyVersion}/retire`, {
      body: payload
    });
  }

  getSegmentOverrides(policyKey: string): Promise<ApiResult<Record<string, number>>> {
    return this.request<Record<string, number>>('GET', `/xp/policies/${policyKey}/segments`);
  }

  updateSegmentOverrides(
    policyKey: string,
    overrides: Record<string, number>
  ): Promise<ApiResult<Record<string, number>>> {
    return this.request<Record<string, number>>('PUT', `/xp/policies/${policyKey}/segments`, {
      body: { overrides }
    });
  }

  private async request<T>(
    method: string,
    path: string,
    options?: { body?: unknown; params?: HttpParams }
  ): Promise<ApiResult<T>> {
    const headers = this.buildHeaders();
    try {
      const response = await firstValueFrom(
        this.http.request<T>(method, `${this.baseUrl}${path}`, {
          body: options?.body,
          params: options?.params,
          headers,
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

  private buildHeaders(): HttpHeaders {
    let headers = new HttpHeaders();
    if (environment.apiAuthToken) {
      const headerName = environment.apiAuthHeaderName || 'Authorization';
      headers = headers.set(headerName, environment.apiAuthToken);
    }

    return headers;
  }

  private buildParams(values: Record<string, string | number | undefined | null>): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(values)) {
      if (value === undefined || value === null) continue;
      params = params.set(key, value.toString());
    }
    return params;
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
