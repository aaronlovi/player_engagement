using System;
using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Represents persisted streak state for a user.
/// </summary>
public sealed record StreakState
{
    /// <summary>Creates a new streak state.</summary>
    /// <param name="CurrentStreak">Current streak day (minimum 0 for unset).</param>
    /// <param name="LongestStreak">Longest streak achieved so far.</param>
    /// <param name="GraceUsed">Number of grace misses already consumed.</param>
    /// <param name="LastRewardDayId">Last reward-day id claimed (DateOnly in anchor timezone).</param>
    /// <param name="ModelState">Model-specific runtime state (e.g., milestones).</param>
    public StreakState(
        int currentStreak,
        int longestStreak,
        int graceUsed,
        DateOnly? lastRewardDayId,
        StreakModelRuntimeState modelState)
    {
        if (currentStreak < 0)
            throw new ArgumentOutOfRangeException(nameof(currentStreak), currentStreak, "CurrentStreak cannot be negative.");
        if (longestStreak < 0)
            throw new ArgumentOutOfRangeException(nameof(longestStreak), longestStreak, "LongestStreak cannot be negative.");
        if (graceUsed < 0)
            throw new ArgumentOutOfRangeException(nameof(graceUsed), graceUsed, "GraceUsed cannot be negative.");
        if (modelState == null)
            modelState = StreakModelRuntimeState.Empty;

        CurrentStreak = currentStreak;
        LongestStreak = longestStreak;
        GraceUsed = graceUsed;
        LastRewardDayId = lastRewardDayId;
        ModelState = modelState;
    }

    /// <summary>Current streak day (minimum 0 for unset).</summary>
    public int CurrentStreak { get; }

    /// <summary>Longest streak achieved so far.</summary>
    public int LongestStreak { get; }

    /// <summary>Number of grace misses already consumed.</summary>
    public int GraceUsed { get; }

    /// <summary>Last reward-day id claimed (DateOnly in anchor timezone).</summary>
    public DateOnly? LastRewardDayId { get; }

    /// <summary>Model-specific runtime state (e.g., milestones).</summary>
    public StreakModelRuntimeState ModelState { get; }
}
