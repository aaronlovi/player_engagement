using System;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Input context for streak transition evaluation.
/// </summary>
public sealed record StreakTransitionContext
{
    /// <summary>Creates a new transition context.</summary>
    /// <param name="policy">Policy document driving streak behavior.</param>
    /// <param name="priorState">Prior streak state for the user.</param>
    /// <param name="rewardDayId">Current reward-day id (DateOnly in anchor timezone).</param>
    /// <param name="claimTimestampUtc">Claim timestamp in UTC (used for observability and ordering).</param>
    public StreakTransitionContext(
        PolicyDocument policy,
        StreakState priorState,
        DateOnly rewardDayId,
        DateTime claimTimestampUtc,
        SeasonBoundaryInfo? seasonBoundary = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(priorState);

        Policy = policy;
        PriorState = priorState;
        RewardDayId = rewardDayId;
        ClaimTimestampUtc = claimTimestampUtc;
        SeasonBoundary = seasonBoundary;
    }

    /// <summary>Policy document driving streak behavior.</summary>
    public PolicyDocument Policy { get; }

    /// <summary>Prior streak state for the user.</summary>
    public StreakState PriorState { get; }

    /// <summary>Current reward-day id (DateOnly in anchor timezone).</summary>
    public DateOnly RewardDayId { get; }

    /// <summary>Claim timestamp in UTC (used for observability and ordering).</summary>
    public DateTime ClaimTimestampUtc { get; }

    /// <summary>Optional season boundary info (when using tiered seasonal reset).</summary>
    public SeasonBoundaryInfo? SeasonBoundary { get; }
}
