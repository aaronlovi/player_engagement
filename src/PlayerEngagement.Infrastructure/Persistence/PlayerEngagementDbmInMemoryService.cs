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

    public Task<Result<long>> CreatePolicyDraftAsync(
        PolicyVersionWriteDto dto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streak,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct) {

        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(streak);
        ArgumentNullException.ThrowIfNull(boosts);

        Result<long> result = PlayerEngagementDbmInMemoryData.CreatePolicyDraft(dto, streak, boosts);
        return Task.FromResult(result);
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
