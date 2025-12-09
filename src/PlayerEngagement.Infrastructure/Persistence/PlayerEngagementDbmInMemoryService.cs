using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;
using PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Persistence.DTOs.Seasons;

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

        _logger.LogInformation(
            "InMemory CreatePolicyDraftAsync start: policyKey={PolicyKey}, requestedVersion={RequestedVersion}, streakCount={StreakCount}, boostCount={BoostCount}",
            dto.PolicyKey,
            dto.PolicyVersion,
            streak.Count,
            boosts.Count);

        if (dto is null) {
            _logger.LogError("InMemory CreatePolicyDraftAsync failed: dto is null");
            return Result<long>.Failure(ErrorCodes.ValidationError, "Request payload is required.");
        }

        if (streak is null) {
            _logger.LogError("InMemory CreatePolicyDraftAsync failed: streak collection is null");
            return Result<long>.Failure(ErrorCodes.ValidationError, "Streak entries are required.");
        }

        if (boosts is null) {
            _logger.LogError("InMemory CreatePolicyDraftAsync failed: seasonal boosts collection is null");
            return Result<long>.Failure(ErrorCodes.ValidationError, "Seasonal boosts collection is required.");
        }

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
        if (result.IsFailure)
            _logger.LogError("InMemory CreatePolicyDraftAsync failed: {Error}", result.ErrorMessage);
        else
            _logger.LogInformation("InMemory CreatePolicyDraftAsync complete: policyKey={PolicyKey}, version={Version}", dto.PolicyKey, result.Value);
        return result;
    }

    public async Task<Result<PolicyVersionDTO>> PublishPolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime publishedAt,
        DateTime? effectiveAt,
        IReadOnlyList<PolicySegmentOverrideDTO> segmentOverrides,
        CancellationToken ct) {

        if (segmentOverrides is null) {
            _logger.LogError("InMemory PublishPolicyVersionAsync failed: segmentOverrides is null for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
            return Result<PolicyVersionDTO>.Failure(ErrorCodes.ValidationError, "Segment overrides collection is required.");
        }

        _logger.LogInformation(
            "InMemory PublishPolicyVersionAsync start: policyKey={PolicyKey}, version={Version}, effectiveAt={EffectiveAt}, overrides={OverrideCount}",
            policyKey,
            policyVersion,
            effectiveAt,
            segmentOverrides.Count);

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

        if (result.IsFailure)
            _logger.LogError("InMemory PublishPolicyVersionAsync failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, result.ErrorMessage);
        else
            _logger.LogInformation("InMemory PublishPolicyVersionAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return result;
    }

    public Task<Result<PolicyVersionDTO>> RetirePolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime retiredAt,
        CancellationToken ct) {

        _logger.LogInformation("InMemory RetirePolicyVersionAsync start: policyKey={PolicyKey}, version={Version}, retiredAt={RetiredAt}", policyKey, policyVersion, retiredAt);
        Result<PolicyVersionDTO> result = PlayerEngagementDbmInMemoryData.RetirePolicyVersion(policyKey, policyVersion, retiredAt);
        if (result.IsFailure)
            _logger.LogError("InMemory RetirePolicyVersionAsync failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, result.ErrorMessage);
        else
            _logger.LogInformation("InMemory RetirePolicyVersionAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return Task.FromResult(result);
    }

    public async Task<Result> ReplacePolicyStreakCurveAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicyStreakCurveEntryDTO> entries,
        CancellationToken ct) {

        if (entries is null) {
            _logger.LogError("InMemory ReplacePolicyStreakCurveAsync failed: entries is null for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
            return Result.Failure(ErrorCodes.ValidationError, "Streak entries are required.");
        }
        _logger.LogInformation("InMemory ReplacePolicyStreakCurveAsync start: policyKey={PolicyKey}, version={Version}, count={Count}", policyKey, policyVersion, entries.Count);
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
        if (result.IsFailure)
            _logger.LogError("InMemory ReplacePolicyStreakCurveAsync failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, result.ErrorMessage);
        else
            _logger.LogInformation("InMemory ReplacePolicyStreakCurveAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return result;
    }

    public async Task<Result> ReplacePolicySeasonalBoostsAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct) {

        if (boosts is null) {
            _logger.LogError("InMemory ReplacePolicySeasonalBoostsAsync failed: boosts is null for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
            return Result.Failure(ErrorCodes.ValidationError, "Seasonal boosts collection is required.");
        }
        _logger.LogInformation("InMemory ReplacePolicySeasonalBoostsAsync start: policyKey={PolicyKey}, version={Version}, count={Count}", policyKey, policyVersion, boosts.Count);
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
        if (result.IsFailure)
            _logger.LogError("InMemory ReplacePolicySeasonalBoostsAsync failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, result.ErrorMessage);
        else
            _logger.LogInformation("InMemory ReplacePolicySeasonalBoostsAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return result;
    }

    public async Task<Result> UpsertPolicySegmentOverridesAsync(
        string policyKey,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides,
        CancellationToken ct) {

        if (overrides is null) {
            _logger.LogError("InMemory UpsertPolicySegmentOverridesAsync failed: overrides is null for policyKey={PolicyKey}", policyKey);
            return Result.Failure(ErrorCodes.ValidationError, "Overrides collection is required.");
        }
        _logger.LogInformation("InMemory UpsertPolicySegmentOverridesAsync start: policyKey={PolicyKey}, count={Count}", policyKey, overrides.Count);
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
        if (result.IsFailure)
            _logger.LogError("InMemory UpsertPolicySegmentOverridesAsync failed for policyKey={PolicyKey}: {Error}", policyKey, result.ErrorMessage);
        else
            _logger.LogInformation("InMemory UpsertPolicySegmentOverridesAsync complete: policyKey={PolicyKey}", policyKey);
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

    public Task<Result<SeasonCalendarWithNextDTO>> GetCurrentSeasonAsync(CancellationToken ct) {
        _logger.LogInformation("InMemory GetCurrentSeasonAsync start.");
        lock (Locker) {
            SeasonCalendarWithNextDTO dto = PlayerEngagementDbmInMemoryData.GetCurrentSeason();
            _logger.LogInformation(
                "InMemory GetCurrentSeasonAsync complete: currentSeasonId={CurrentSeasonId}, nextSeasonId={NextSeasonId}",
                dto.Current.SeasonId,
                dto.Next.SeasonId);
            return Task.FromResult(Result<SeasonCalendarWithNextDTO>.Success(dto));
        }
    }
}
