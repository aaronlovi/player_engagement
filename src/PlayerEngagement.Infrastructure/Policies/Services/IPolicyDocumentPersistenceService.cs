using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlayerEngagement.Domain.Policies;

namespace PlayerEngagement.Infrastructure.Policies.Services;

/// <summary>
/// Provides access to policy documents and related metadata directly from persistence.
/// </summary>
public interface IPolicyDocumentPersistenceService {
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
}
