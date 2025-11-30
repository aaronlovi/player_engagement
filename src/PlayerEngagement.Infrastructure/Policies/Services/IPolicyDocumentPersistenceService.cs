using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Policies.Services;

/// <summary>
/// Provides access to policy documents and related metadata directly from persistence.
/// </summary>
public interface IPolicyDocumentPersistenceService {
    /// <summary>
    /// Creates a new draft policy version for the supplied key.
    /// </summary>
    /// <param name="dto">Write DTO used to store the policy version.</param>
    /// <param name="streak">Streak curve entries associated with the version.</param>
    /// <param name="boosts">Seasonal boosts tied to the version.</param>
    /// <param name="ct">Cancellation token tied to the persistence operation.</param>
    /// <returns>Result containing the created <see cref="PolicyDocument"/> when successful.</returns>
    Task<Result<PolicyDocument>> CreatePolicyDraftAsync(
        PolicyVersionWriteDto dto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streak,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct);

    /// <summary>
    /// Publishes a draft or archived policy version and optionally applies segment overrides.
    /// </summary>
    /// <param name="policyKey">Logical policy identifier.</param>
    /// <param name="policyVersion">Version number to publish.</param>
    /// <param name="publishedAt">Timestamp when the publish was requested.</param>
    /// <param name="effectiveAt">Optional effective timestamp.</param>
    /// <param name="segmentOverrides">Overrides to apply for segments.</param>
    /// <param name="ct">Cancellation token tied to the persistence operation.</param>
    /// <returns>Result containing the published <see cref="PolicyDocument"/>.</returns>
    Task<Result<PolicyDocument>> PublishPolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime publishedAt,
        DateTime? effectiveAt,
        IReadOnlyList<PolicySegmentOverrideDTO> segmentOverrides,
        CancellationToken ct);

    /// <summary>
    /// Retires a published policy version.
    /// </summary>
    /// <param name="policyKey">Logical policy identifier.</param>
    /// <param name="policyVersion">Version number to retire.</param>
    /// <param name="retiredAt">Retirement timestamp.</param>
    /// <param name="ct">Cancellation token tied to the persistence operation.</param>
    /// <returns>Result containing the retired version metadata.</returns>
    Task<Result<PolicyVersionDocument>> RetirePolicyVersionAsync(string policyKey, long policyVersion, DateTime retiredAt, CancellationToken ct);

    /// <summary>
    /// Lists policy version metadata for a given key.
    /// </summary>
    /// <param name="policyKey">Logical policy identifier.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="effectiveBefore">Optional effective timestamp filter.</param>
    /// <param name="limit">Optional maximum rows to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of policy version documents.</returns>
    Task<IReadOnlyList<PolicyVersionDocument>> ListPolicyVersionsAsync(string policyKey, string? status, DateTime? effectiveBefore, int? limit, CancellationToken ct);

    /// <summary>
    /// Retrieves the policy that is effective for the specified key at the supplied UTC instant.
    /// </summary>
    /// <param name="policyKey">Logical identifier for the policy family (e.g., daily login).</param>
    /// <param name="utcNow">UTC timestamp used to resolve the currently active policy version.</param>
    /// <param name="ct">Cancellation token that aborts the database call when triggered.</param>
    /// <returns>The active <see cref="PolicyDocument"/> or <c>null</c> when none exist.</returns>
    Task<PolicyDocument?> GetCurrentPolicyAsync(string policyKey, DateTime utcNow, CancellationToken ct);

    /// <summary>
    /// Retrieves a specific policy version by key and version number regardless of current effectiveness.
    /// </summary>
    /// <param name="policyKey">Logical identifier for the policy family.</param>
    /// <param name="policyVersion">Version number that uniquely identifies the saved policy document.</param>
    /// <param name="ct">Cancellation token for the underlying database operation.</param>
    /// <returns>The requested <see cref="PolicyDocument"/> or <c>null</c> if the version cannot be found.</returns>
    Task<PolicyDocument?> GetPolicyVersionAsync(string policyKey, long policyVersion, CancellationToken ct);

    /// <summary>
    /// Lists all published policy versions that are effective on or before the supplied UTC instant.
    /// </summary>
    /// <param name="utcNow">UTC timestamp filter to determine which versions are considered published.</param>
    /// <param name="ct">Cancellation token for the loaded query.</param>
    /// <returns>A read-only list of published policy documents.</returns>
    Task<IReadOnlyList<PolicyDocument>> ListPublishedPoliciesAsync(DateTime utcNow, CancellationToken ct);

    /// <summary>
    /// Retrieves the policy version overrides defined for each player segment under the provided policy key.
    /// </summary>
    /// <param name="policyKey">Logical identifier for the policy family that owns the overrides.</param>
    /// <param name="ct">Cancellation token for the retrieval request.</param>
    /// <returns>A read-only dictionary keyed by segment identifier with policy version values.</returns>
    Task<IReadOnlyDictionary<string, long>> GetSegmentOverridesAsync(string policyKey, CancellationToken ct);

    /// <summary>
    /// Replaces the segment override mappings for the specified policy key.
    /// </summary>
    /// <param name="policyKey">Logical identifier for the policy family.</param>
    /// <param name="overrides">Overrides to persist.</param>
    /// <param name="ct">Cancellation token for the persistence call.</param>
    /// <returns>Result with the stored override map.</returns>
    Task<Result<IReadOnlyDictionary<string, long>>> UpdateSegmentOverridesAsync(string policyKey, IReadOnlyList<PolicySegmentOverrideDTO> overrides, CancellationToken ct);
}
