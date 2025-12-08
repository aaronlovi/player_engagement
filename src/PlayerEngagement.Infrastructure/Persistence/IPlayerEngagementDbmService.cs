using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Shared;
using PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Persistence.DTOs.Seasons;

namespace PlayerEngagement.Infrastructure.Persistence;

public interface IPlayerEngagementDbmService : IDbmService {
    Task<Result> HealthCheckAsync(CancellationToken ct);

    Task<Result<long>> CreatePolicyDraftAsync(
        PolicyVersionWriteDto dto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streak,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct);

    Task<Result<PolicyVersionDTO>> PublishPolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime publishedAt,
        DateTime? effectiveAt,
        IReadOnlyList<PolicySegmentOverrideDTO> segmentOverrides,
        CancellationToken ct);

    Task<Result<PolicyVersionDTO>> RetirePolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime retiredAt,
        CancellationToken ct);

    Task<Result> ReplacePolicyStreakCurveAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicyStreakCurveEntryDTO> entries,
        CancellationToken ct);

    Task<Result> ReplacePolicySeasonalBoostsAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct);

    Task<Result> UpsertPolicySegmentOverridesAsync(
        string policyKey,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides,
        CancellationToken ct);

    Task<Result<List<PolicyVersionDTO>>> ListPolicyVersionsAsync(
        string policyKey,
        string? status,
        DateTime? effectiveBefore,
        int? limit,
        CancellationToken ct);

    Task<Result<ActivePolicyDTO>> GetCurrentPolicyAsync(string policyKey, DateTime utcNow, CancellationToken ct);
    Task<Result<PolicyVersionDTO>> GetPolicyVersionAsync(string policyKey, long policyVersion, CancellationToken ct);
    Task<Result<List<PolicyVersionDTO>>> ListPublishedPoliciesAsync(DateTime utcNow, CancellationToken ct);
    Task<Result<List<PolicyStreakCurveEntryDTO>>> GetPolicyStreakCurveAsync(string policyKey, long policyVersion, CancellationToken ct);
    Task<Result<List<PolicySeasonalBoostDTO>>> GetPolicySeasonalBoostsAsync(string policyKey, long policyVersion, CancellationToken ct);
    Task<Result<List<PolicySegmentOverrideDTO>>> GetPolicySegmentOverridesAsync(string policyKey, CancellationToken ct);
    Task<Result<SeasonCalendarDTO>> GetCurrentSeasonAsync(CancellationToken ct);
}
