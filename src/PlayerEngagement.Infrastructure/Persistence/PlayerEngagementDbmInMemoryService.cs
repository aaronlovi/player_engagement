using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence;

public sealed class PlayerEngagementDbmInMemoryService : DbmInMemoryService, IPlayerEngagementDbmService, IDbmService {
    private readonly ILogger<PlayerEngagementDbmInMemoryService> _logger;

    public PlayerEngagementDbmInMemoryService(ILoggerFactory loggerFactory) : base(loggerFactory) {
        _logger = loggerFactory.CreateLogger<PlayerEngagementDbmInMemoryService>();
        _logger.LogWarning("PlayerEngagementDbmInMemoryService instantiated: persistence in RAM only");
    }

    public Task<Result> HealthCheckAsync(CancellationToken ct) {
        _logger.LogInformation("Player Engagement in-memory DBM health check requested.");
        return Task.FromResult(Result.Success);
    }

    public async Task<Result<long>> CreatePolicyDraftAsync(
        PolicyVersionWriteDto dto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streak,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct) {

        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(streak);
        ArgumentNullException.ThrowIfNull(boosts);

        long policyVersion = dto.PolicyVersion != 0 ? dto.PolicyVersion : (long)await GetNextId64(ct);
        long policyId = dto.PolicyId ?? (long)await GetNextId64(ct);

        List<PolicyStreakCurveEntryDTO> normalizedStreak = new(streak.Count);
        foreach (PolicyStreakCurveEntryDTO entry in streak) {
            long streakCurveId = entry.StreakCurveId != 0 ? entry.StreakCurveId : (long)await GetNextId64(ct);
            normalizedStreak.Add(new PolicyStreakCurveEntryDTO(
                streakCurveId,
                dto.PolicyKey,
                policyVersion,
                entry.DayIndex,
                entry.Multiplier,
                entry.AdditiveBonusXp,
                entry.CapNextDay));
        }

        List<PolicySeasonalBoostDTO> normalizedBoosts = new(boosts.Count);
        foreach (PolicySeasonalBoostDTO boost in boosts) {
            long boostId = boost.BoostId != 0 ? boost.BoostId : (long)await GetNextId64(ct);
            normalizedBoosts.Add(new PolicySeasonalBoostDTO(
                boostId,
                dto.PolicyKey,
                policyVersion,
                boost.Label,
                boost.Multiplier,
                boost.StartUtc,
                boost.EndUtc));
        }

        PolicyVersionWriteDto expandedDto = dto with { PolicyVersion = policyVersion, PolicyId = policyId };

        Result<long> result = PlayerEngagementDbmInMemoryData.CreatePolicyDraft(expandedDto, normalizedStreak, normalizedBoosts);
        return result;
    }

    public async Task<Result<PolicyVersionDTO>> PublishPolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime publishedAt,
        DateTime? effectiveAt,
        IReadOnlyList<PolicySegmentOverrideDTO> segmentOverrides,
        CancellationToken ct) {

        ArgumentNullException.ThrowIfNull(segmentOverrides);

        List<PolicySegmentOverrideDTO> overrides = new(segmentOverrides.Count);
        foreach (PolicySegmentOverrideDTO dto in segmentOverrides) {
            long overrideId = dto.OverrideId != 0 ? dto.OverrideId : (long)await GetNextId64(ct);
            overrides.Add(new PolicySegmentOverrideDTO(
                overrideId,
                dto.SegmentKey,
                policyKey,
                dto.TargetPolicyVersion,
                dto.CreatedAt,
                dto.CreatedBy));
        }

        Result<PolicyVersionDTO> result = PlayerEngagementDbmInMemoryData.PublishPolicyVersion(
            policyKey,
            policyVersion,
            publishedAt,
            effectiveAt,
            overrides);

        return result;
    }

    public Task<Result<PolicyVersionDTO>> RetirePolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime retiredAt,
        CancellationToken ct) {

        Result<PolicyVersionDTO> result = PlayerEngagementDbmInMemoryData.RetirePolicyVersion(policyKey, policyVersion, retiredAt);
        return Task.FromResult(result);
    }

    public async Task<Result> ReplacePolicyStreakCurveAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicyStreakCurveEntryDTO> entries,
        CancellationToken ct) {

        ArgumentNullException.ThrowIfNull(entries);
        List<PolicyStreakCurveEntryDTO> normalized = new(entries.Count);

        foreach (PolicyStreakCurveEntryDTO entry in entries) {
            long streakCurveId = entry.StreakCurveId != 0 ? entry.StreakCurveId : (long)await GetNextId64(ct);
            normalized.Add(new PolicyStreakCurveEntryDTO(
                streakCurveId,
                policyKey,
                policyVersion,
                entry.DayIndex,
                entry.Multiplier,
                entry.AdditiveBonusXp,
                entry.CapNextDay));
        }

        Result result = PlayerEngagementDbmInMemoryData.ReplaceStreakCurve(policyKey, policyVersion, normalized);
        return result;
    }

    public async Task<Result> ReplacePolicySeasonalBoostsAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct) {

        ArgumentNullException.ThrowIfNull(boosts);
        List<PolicySeasonalBoostDTO> normalized = new(boosts.Count);

        foreach (PolicySeasonalBoostDTO boost in boosts) {
            long boostId = boost.BoostId != 0 ? boost.BoostId : (long)await GetNextId64(ct);
            normalized.Add(new PolicySeasonalBoostDTO(
                boostId,
                policyKey,
                policyVersion,
                boost.Label,
                boost.Multiplier,
                boost.StartUtc,
                boost.EndUtc));
        }

        Result result = PlayerEngagementDbmInMemoryData.ReplaceSeasonalBoosts(policyKey, policyVersion, normalized);
        return result;
    }

    public async Task<Result> UpsertPolicySegmentOverridesAsync(
        string policyKey,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides,
        CancellationToken ct) {

        ArgumentNullException.ThrowIfNull(overrides);
        List<PolicySegmentOverrideDTO> normalized = new(overrides.Count);

        foreach (PolicySegmentOverrideDTO dto in overrides) {
            long overrideId = dto.OverrideId != 0 ? dto.OverrideId : (long)await GetNextId64(ct);
            normalized.Add(new PolicySegmentOverrideDTO(
                overrideId,
                dto.SegmentKey,
                policyKey,
                dto.TargetPolicyVersion,
                dto.CreatedAt,
                dto.CreatedBy));
        }

        Result result = PlayerEngagementDbmInMemoryData.UpsertSegmentOverrides(policyKey, normalized);
        return result;
    }

    public Task<Result<List<PolicyVersionDTO>>> ListPolicyVersionsAsync(
        string policyKey,
        string? status,
        DateTime? effectiveBefore,
        int? limit,
        CancellationToken ct) {

        List<PolicyVersionDTO> versions = PlayerEngagementDbmInMemoryData.ListPolicyVersions(policyKey, status, effectiveBefore, limit);
        return Task.FromResult(Result<List<PolicyVersionDTO>>.Success(versions));
    }

    public Task<Result<ActivePolicyDTO>> GetCurrentPolicyAsync(string policyKey, DateTime utcNow, CancellationToken ct) {
        lock (Locker) {
            if (!PlayerEngagementDbmInMemoryData.TryGetActivePolicy(policyKey, utcNow, out ActivePolicyDTO dto))
                return Task.FromResult(Result<ActivePolicyDTO>.Failure(ErrorCodes.NotFound, $"Active policy '{policyKey}' not found."));

            return Task.FromResult(Result<ActivePolicyDTO>.Success(dto));
        }
    }

    public Task<Result<PolicyVersionDTO>> GetPolicyVersionAsync(string policyKey, long policyVersion, CancellationToken ct) {
        lock (Locker) {
            if (!PlayerEngagementDbmInMemoryData.TryGetPolicyVersion(policyKey, policyVersion, out PolicyVersionDTO dto))
                return Task.FromResult(Result<PolicyVersionDTO>.Failure(ErrorCodes.NotFound, $"Policy '{policyKey}' version '{policyVersion}' not found."));

            return Task.FromResult(Result<PolicyVersionDTO>.Success(dto));
        }
    }

    public Task<Result<List<PolicyVersionDTO>>> ListPublishedPoliciesAsync(DateTime utcNow, CancellationToken ct) {
        lock (Locker) {
            List<PolicyVersionDTO> policies = PlayerEngagementDbmInMemoryData.ListPublishedPolicies(utcNow);
            return Task.FromResult(Result<List<PolicyVersionDTO>>.Success(policies));
        }
    }

    public Task<Result<List<PolicyStreakCurveEntryDTO>>> GetPolicyStreakCurveAsync(string policyKey, long policyVersion, CancellationToken ct) {
        lock (Locker) {
            List<PolicyStreakCurveEntryDTO> entries = PlayerEngagementDbmInMemoryData.GetStreakCurve(policyKey, policyVersion);
            return Task.FromResult(Result<List<PolicyStreakCurveEntryDTO>>.Success(entries));
        }
    }

    public Task<Result<List<PolicySeasonalBoostDTO>>> GetPolicySeasonalBoostsAsync(string policyKey, long policyVersion, CancellationToken ct) {
        lock (Locker) {
            List<PolicySeasonalBoostDTO> boosts = PlayerEngagementDbmInMemoryData.GetSeasonalBoosts(policyKey, policyVersion);
            return Task.FromResult(Result<List<PolicySeasonalBoostDTO>>.Success(boosts));
        }
    }

    public Task<Result<List<PolicySegmentOverrideDTO>>> GetPolicySegmentOverridesAsync(string policyKey, CancellationToken ct) {
        lock (Locker) {
            List<PolicySegmentOverrideDTO> overrides = PlayerEngagementDbmInMemoryData.GetSegmentOverrides(policyKey);
            return Task.FromResult(Result<List<PolicySegmentOverrideDTO>>.Success(overrides));
        }
    }
}
