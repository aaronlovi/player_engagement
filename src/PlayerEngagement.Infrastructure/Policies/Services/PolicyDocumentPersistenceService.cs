using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Infrastructure.Persistence;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Infrastructure.Policies.Mappers;

namespace PlayerEngagement.Infrastructure.Policies.Services;

/// <summary>
/// Persistence-facing service that assembles domain <see cref="PolicyDocument"/> instances from DTOs.
/// </summary>
public sealed class PolicyDocumentPersistenceService : IPolicyDocumentPersistenceService {
    private readonly IPlayerEngagementDbmService _dbm;
    private readonly ILogger<PolicyDocumentPersistenceService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyDocumentPersistenceService"/> class.
    /// </summary>
    /// <param name="dbm">Database module service that executes policy queries.</param>
    /// <param name="logger">Typed logger used to record operational concerns.</param>
    public PolicyDocumentPersistenceService(IPlayerEngagementDbmService dbm, ILoggerFactory loggerFactory) {
        _dbm = dbm ?? throw new ArgumentNullException(nameof(dbm));
        _logger = loggerFactory.CreateLogger<PolicyDocumentPersistenceService>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public async Task<PolicyDocument?> GetCurrentPolicyAsync(string policyKey, DateTime utcNow, CancellationToken ct) {
        Result<ActivePolicyDTO> policyResult = await _dbm.GetCurrentPolicyAsync(policyKey, utcNow, ct);
        if (policyResult.IsFailure || policyResult.Value is null || policyResult.Value.IsEmpty)
            return null;

        ActivePolicyDTO dto = policyResult.Value;
        List<PolicyStreakCurveEntryDTO> streak = await FetchStreakCurveAsync(policyKey, dto.PolicyVersion, ct);
        List<PolicySeasonalBoostDTO> boosts = await FetchSeasonalBoostsAsync(policyKey, dto.PolicyVersion, ct);
        return PolicyDocumentMapper.ToDomain(dto, streak, boosts);
    }

    /// <inheritdoc />
    public async Task<PolicyDocument?> GetPolicyVersionAsync(string policyKey, int policyVersion, CancellationToken ct) {
        Result<PolicyVersionDTO> policyResult = await _dbm.GetPolicyVersionAsync(policyKey, policyVersion, ct);
        if (policyResult.IsFailure || policyResult.Value is null || policyResult.Value.IsEmpty)
            return null;

        PolicyVersionDTO dto = policyResult.Value;
        List<PolicyStreakCurveEntryDTO> streak = await FetchStreakCurveAsync(policyKey, policyVersion, ct);
        List<PolicySeasonalBoostDTO> boosts = await FetchSeasonalBoostsAsync(policyKey, policyVersion, ct);
        return PolicyDocumentMapper.ToDomain(dto, streak, boosts);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyDocument>> ListPublishedPoliciesAsync(DateTime utcNow, CancellationToken ct) {
        Result<List<PolicyVersionDTO>> result = await _dbm.ListPublishedPoliciesAsync(utcNow, ct);
        if (result.IsFailure || result.Value is null)
            return Array.Empty<PolicyDocument>();

        List<PolicyDocument> documents = new(result.Value.Count);
        foreach (PolicyVersionDTO dto in result.Value) {
            List<PolicyStreakCurveEntryDTO> streak = await FetchStreakCurveAsync(dto.PolicyKey, dto.PolicyVersion, ct);
            List<PolicySeasonalBoostDTO> boosts = await FetchSeasonalBoostsAsync(dto.PolicyKey, dto.PolicyVersion, ct);
            documents.Add(PolicyDocumentMapper.ToDomain(dto, streak, boosts));
        }

        return documents;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetSegmentOverridesAsync(string policyKey, CancellationToken ct) {
        Result<List<PolicySegmentOverrideDTO>> result = await _dbm.GetPolicySegmentOverridesAsync(policyKey, ct);
        if (result.IsFailure || result.Value is null)
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        return PolicySegmentOverrideMapper.ToDictionary(result.Value);
    }

    #region PRIVATE HELPER METHODS

    /// <summary>
    /// Loads the streak curve rows backing the specified policy and version.
    /// </summary>
    /// <param name="policyKey">Policy family identifier.</param>
    /// <param name="policyVersion">Version number to evaluate.</param>
    /// <param name="ct">Cancellation token tied to the database read.</param>
    /// <returns>Collection of streak curve DTO rows, or an empty list when none exist.</returns>
    private async Task<List<PolicyStreakCurveEntryDTO>> FetchStreakCurveAsync(string policyKey, int policyVersion, CancellationToken ct) {
        Result<List<PolicyStreakCurveEntryDTO>> result = await _dbm.GetPolicyStreakCurveAsync(policyKey, policyVersion, ct);
        if (result.IsFailure || result.Value is null)
            return [];

        return result.Value;
    }

    /// <summary>
    /// Loads the seasonal boost rows for the specified policy version.
    /// </summary>
    /// <param name="policyKey">Policy family identifier.</param>
    /// <param name="policyVersion">Version number to evaluate.</param>
    /// <param name="ct">Cancellation token tied to the database read.</param>
    /// <returns>Collection of seasonal boost DTO rows, or an empty list when none exist.</returns>
    private async Task<List<PolicySeasonalBoostDTO>> FetchSeasonalBoostsAsync(string policyKey, int policyVersion, CancellationToken ct) {
        Result<List<PolicySeasonalBoostDTO>> result = await _dbm.GetPolicySeasonalBoostsAsync(policyKey, policyVersion, ct);
        if (result.IsFailure || result.Value is null)
            return [];

        return result.Value;
    }

    #endregion PRIVATE HELPER METHODS
}
