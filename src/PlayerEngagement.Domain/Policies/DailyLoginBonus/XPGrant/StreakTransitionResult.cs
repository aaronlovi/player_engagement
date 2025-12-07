using System;
using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Result of applying a streak model transition.
/// </summary>
public sealed record StreakTransitionResult
{
    /// <summary>Creates a new transition result.</summary>
    /// <param name="state">New streak state to persist.</param>
    /// <param name="effectiveStreakDay">Effective streak day after model rules.</param>
    /// <param name="effectiveMultiplier">Final XP multiplier (curve * model multiplier).</param>
    /// <param name="additiveBonusXp">Additive XP applied from curve/model.</param>
    /// <param name="xpAwarded">Total XP awarded (rounded, additive applied).</param>
    /// <param name="graceApplied">True if grace covered missed days.</param>
    /// <param name="milestoneHits">Milestone days unlocked by this claim.</param>
    public StreakTransitionResult(
        StreakState state,
        int effectiveStreakDay,
        decimal effectiveMultiplier,
        int additiveBonusXp,
        int xpAwarded,
        bool graceApplied,
        IReadOnlyCollection<MilestoneMetaRewardMilestone> milestoneHits)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(milestoneHits);
        if (effectiveStreakDay < 1)
            throw new ArgumentOutOfRangeException(nameof(effectiveStreakDay), effectiveStreakDay, "Effective streak day must be at least 1.");
        if (effectiveMultiplier <= 0m)
            throw new ArgumentOutOfRangeException(nameof(effectiveMultiplier), effectiveMultiplier, "Effective multiplier must be greater than 0.");

        State = state;
        EffectiveStreakDay = effectiveStreakDay;
        EffectiveMultiplier = effectiveMultiplier;
        AdditiveBonusXp = additiveBonusXp;
        XpAwarded = xpAwarded;
        GraceApplied = graceApplied;
        MilestoneHits = milestoneHits;
    }

    /// <summary>New streak state to persist.</summary>
    public StreakState State { get; }

    /// <summary>Effective streak day after model rules.</summary>
    public int EffectiveStreakDay { get; }

    /// <summary>Final XP multiplier (curve * model multiplier).</summary>
    public decimal EffectiveMultiplier { get; }

    /// <summary>
    /// Additive XP applied from curve/model; informational for reporting/UI.
    /// This amount is already included in XpAwarded.
    /// </summary>
    public int AdditiveBonusXp { get; }

    /// <summary>Total XP awarded (rounded, additive applied).</summary>
    public int XpAwarded { get; }

    /// <summary>True if grace covered missed days.</summary>
    public bool GraceApplied { get; }

    /// <summary>Milestone days unlocked by this claim.</summary>
    public IReadOnlyCollection<MilestoneMetaRewardMilestone> MilestoneHits { get; }
}
