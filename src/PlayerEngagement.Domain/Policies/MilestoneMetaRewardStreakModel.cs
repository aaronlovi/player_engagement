using System;
using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Milestone/meta reward streak model with milestone definitions.
/// </summary>
public sealed record MilestoneMetaRewardStreakModel : StreakModelDefinition {
    public MilestoneMetaRewardStreakModel(IReadOnlyList<MilestoneMetaRewardMilestone> milestones)
        : base(StreakModelType.MilestoneMetaReward) {
        ArgumentNullException.ThrowIfNull(milestones);
        if (milestones.Count == 0)
            throw new ArgumentException("At least one milestone is required.", nameof(milestones));

        Milestones = milestones;
    }

    public IReadOnlyList<MilestoneMetaRewardMilestone> Milestones { get; }
}
