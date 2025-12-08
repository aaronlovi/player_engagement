using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Represents milestone evaluation results for a streak computation.
/// </summary>
/// <param name="Hits">Milestone entries that triggered on the current claim.</param>
/// <param name="ModelState">Updated model state after applying milestone decisions.</param>
public sealed record MilestoneEvaluation(
    IReadOnlyCollection<MilestoneMetaRewardMilestone> Hits,
    StreakModelRuntimeState ModelState);
