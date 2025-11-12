using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence;

public interface IPlayerEngagementDbmService {
    Task<Result> HealthCheckAsync(CancellationToken ct);

    Task<Result<long>> CreatePolicyDraftAsync(
        PolicyVersionWriteDto dto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streak,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct);

    Task<Result<ActivePolicyDTO>> GetCurrentPolicyAsync(string policyKey, DateTime utcNow, CancellationToken ct);
    Task<Result<PolicyVersionDTO>> GetPolicyVersionAsync(string policyKey, long policyVersion, CancellationToken ct);
    Task<Result<List<PolicyVersionDTO>>> ListPublishedPoliciesAsync(DateTime utcNow, CancellationToken ct);
    Task<Result<List<PolicyStreakCurveEntryDTO>>> GetPolicyStreakCurveAsync(string policyKey, long policyVersion, CancellationToken ct);
    Task<Result<List<PolicySeasonalBoostDTO>>> GetPolicySeasonalBoostsAsync(string policyKey, long policyVersion, CancellationToken ct);
    Task<Result<List<PolicySegmentOverrideDTO>>> GetPolicySegmentOverridesAsync(string policyKey, CancellationToken ct);
}
