using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using Xunit;

namespace PlayerEngagement.Domain.Tests.Policies.DailyLoginBonus.XPGrant;

public sealed class StreakEngineMilestoneTests
{
    private const int BaseXp = 100;

    [Fact]
    public void Evaluate_TriggersMilestoneAndUpdatesState()
    {
        StreakEngine engine = new();
        List<MilestoneMetaRewardMilestone> milestones =
        [
            new MilestoneMetaRewardMilestone(3, "badge", "gold"),
            new MilestoneMetaRewardMilestone(5, "badge", "platinum")
        ];
        MilestoneMetaRewardStreakModel model = new(milestones);
        PolicyDocument policy = PolicyDocumentFactory.CreatePolicyDocument(
            version: PolicyDocumentFactory.CreatePolicyVersionDocument(
                baseXpAmount: BaseXp,
                streakModel: model,
                gracePolicy: new GracePolicyDefinition(0, 0)),
            streakCurve:
            [
                new StreakCurveEntry(0, 1m, 0, false),
                new StreakCurveEntry(1, 1m, 0, false),
                new StreakCurveEntry(2, 1m, 0, false),
                new StreakCurveEntry(3, 1m, 0, false)
            ],
            seasonalBoosts: []);

        DateOnly lastDay = new(2024, 1, 1);
        StreakState prior = new(2, 2, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(1); // day 3 should hit milestone
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.Equal(3, result.State.CurrentStreak);
        _ = Assert.Single(result.MilestoneHits);
        Assert.Contains(result.MilestoneHits, m => m.Day == 3);
        Assert.Contains(3, result.State.ModelState.ClaimedMilestones);
    }

    [Fact]
    public void Evaluate_DoesNotReplayMilestoneOnRetry()
    {
        StreakEngine engine = new();
        List<MilestoneMetaRewardMilestone> milestones =
        [
            new MilestoneMetaRewardMilestone(3, "badge", "gold")
        ];
        MilestoneMetaRewardStreakModel model = new(milestones);
        PolicyDocument policy = PolicyDocumentFactory.CreatePolicyDocument(
            version: PolicyDocumentFactory.CreatePolicyVersionDocument(
                baseXpAmount: BaseXp,
                streakModel: model,
                gracePolicy: new GracePolicyDefinition(0, 0)),
            streakCurve:
            [
                new StreakCurveEntry(0, 1m, 0, false),
                new StreakCurveEntry(1, 1m, 0, false),
                new StreakCurveEntry(2, 1m, 0, false)
            ],
            seasonalBoosts: []);

        DateOnly lastDay = new(2024, 1, 1);
        StreakState prior = new(2, 2, 0, lastDay, new StreakModelRuntimeState([3]));

        DateOnly rewardDay = lastDay.AddDays(1); // day 3 already claimed
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.Equal(3, result.State.CurrentStreak);
        Assert.Empty(result.MilestoneHits);
        Assert.Contains(3, result.State.ModelState.ClaimedMilestones);
    }
}
