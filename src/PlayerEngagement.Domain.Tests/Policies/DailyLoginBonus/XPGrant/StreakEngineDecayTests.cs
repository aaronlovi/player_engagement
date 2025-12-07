using System;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using Xunit;

namespace PlayerEngagement.Domain.Tests.Policies.DailyLoginBonus.XPGrant;

public sealed class StreakEngineDecayTests
{
    private const int BaseXp = 100;

    [Fact]
    public void Evaluate_MissTriggersDecay_FloorsAndClamps()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreateDecayPolicy(decayPercent: 0.25m, graceAllowedMisses: 0, graceWindowDays: 0, baseXp: BaseXp);
        DateOnly lastDay = new(2024, 1, 10);
        StreakState prior = new(7, 7, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(2); // one missed day
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.False(result.GraceApplied);
        Assert.Equal(0, result.State.GraceUsed);
        Assert.Equal(5, result.State.CurrentStreak); // floor(7*0.75)=5
        Assert.Equal(7, result.State.LongestStreak); // longest should remain at prior max
        Assert.Equal(140, result.XpAwarded); // base 100 * curve day 5 (1.4)
    }

    [Fact]
    public void Evaluate_MultipleMissesApplyDecayPerMiss()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreateDecayPolicy(decayPercent: 0.25m, graceAllowedMisses: 0, graceWindowDays: 0, baseXp: BaseXp);
        DateOnly lastDay = new(2024, 1, 10);
        StreakState prior = new(8, 8, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(3); // two missed days
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.False(result.GraceApplied);
        Assert.Equal(0, result.State.GraceUsed);
        Assert.Equal(4, result.State.CurrentStreak); // floor(floor(8*0.75)*0.75) = 4
        Assert.Equal(8, result.State.LongestStreak);
        Assert.Equal(130, result.XpAwarded); // base 100 * curve day 4 (1.3)
    }

    [Fact]
    public void Evaluate_GraceCoversMiss_NoDecayApplied()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreateDecayPolicy(decayPercent: 0.25m, graceAllowedMisses: 1, graceWindowDays: 1, baseXp: BaseXp);
        DateOnly lastDay = new(2024, 1, 10);
        StreakState prior = new(3, 3, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(2); // one missed day inside grace
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.True(result.GraceApplied);
        Assert.Equal(1, result.State.GraceUsed);
        Assert.Equal(4, result.State.CurrentStreak); // continues streak
        Assert.Equal(4, result.State.LongestStreak);
        Assert.Equal(130, result.XpAwarded); // base 100 * curve day 4 (1.3)
    }
}
