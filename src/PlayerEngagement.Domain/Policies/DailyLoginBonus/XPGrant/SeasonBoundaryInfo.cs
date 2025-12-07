using System;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Represents the current season window for tiered seasonal streak models.
/// </summary>
public sealed record SeasonBoundaryInfo(DateOnly SeasonStart, DateOnly SeasonEnd)
{
    public bool IsWithinSeason(DateOnly rewardDayId) =>
        rewardDayId >= SeasonStart && rewardDayId <= SeasonEnd;

    public bool HasSeasonEnded(DateOnly rewardDayId) =>
        rewardDayId > SeasonEnd;
}
