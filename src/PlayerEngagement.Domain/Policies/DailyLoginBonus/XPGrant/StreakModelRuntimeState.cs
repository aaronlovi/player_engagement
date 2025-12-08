using System;
using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Model-specific runtime state persisted alongside the streak.
/// </summary>
public sealed record StreakModelRuntimeState
{
    /// <summary>Shared empty instance to avoid allocations.</summary>
    public static readonly StreakModelRuntimeState Empty = new([]);

    /// <summary>Creates a new runtime state.</summary>
    /// <param name="ClaimedMilestones">Milestone days already claimed (for idempotency).</param>
    public StreakModelRuntimeState(IReadOnlyCollection<int> claimedMilestones)
    {
        ArgumentNullException.ThrowIfNull(claimedMilestones);
        ClaimedMilestones = claimedMilestones;
    }

    /// <summary>Milestone days already claimed (for idempotency).</summary>
    public IReadOnlyCollection<int> ClaimedMilestones { get; }
}
