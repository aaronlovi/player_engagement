using System;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Represents a milestone entry for the milestone/meta reward model.
/// </summary>
public sealed record MilestoneMetaRewardMilestone {
    public MilestoneMetaRewardMilestone(int day, string rewardType, string rewardValue) {
        if (day < 1)
            throw new ArgumentOutOfRangeException(nameof(day), day, "Day must be at least 1.");
        if (string.IsNullOrWhiteSpace(rewardType))
            throw new ArgumentException("RewardType is required.", nameof(rewardType));
        if (string.IsNullOrWhiteSpace(rewardValue))
            throw new ArgumentException("RewardValue is required.", nameof(rewardValue));

        Day = day;
        RewardType = rewardType;
        RewardValue = rewardValue;
    }

    public int Day { get; }

    public string RewardType { get; }

    public string RewardValue { get; }
}
