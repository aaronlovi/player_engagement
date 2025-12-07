using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Internal computation container for streak engine evaluation.
/// </summary>
public sealed class StreakEngineModelComputation
{
    public int EffectiveStreakDay { get; set; }

    public decimal ModelMultiplier { get; set; }

    public int AdditiveBonusXp { get; set; }

    public StreakModelRuntimeState ModelState { get; set; } = StreakModelRuntimeState.Empty;

    public IReadOnlyCollection<MilestoneMetaRewardMilestone> MilestoneHits { get; set; } = [];
}
