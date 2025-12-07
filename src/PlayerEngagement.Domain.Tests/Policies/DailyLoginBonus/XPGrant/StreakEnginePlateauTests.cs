using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using Xunit;

namespace PlayerEngagement.Domain.Tests.Policies.DailyLoginBonus.XPGrant;

public sealed class StreakEnginePlateauTests
{
    private const int BaseXp = 100;

    [Fact]
    public void Evaluate_FirstClaim_StartsAtDay1()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreatePlateauPolicy(baseXp: BaseXp);
        StreakState prior = new(0, 0, 0, null, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = new(2024, 1, 1);
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.Equal(1, result.State.CurrentStreak);
        Assert.Equal(1, result.EffectiveStreakDay);
        Assert.Equal(BaseXp, result.XpAwarded);
        Assert.False(result.GraceApplied);
    }

    [Fact]
    public void Evaluate_AdvancesToPlateauAndAppliesMultiplier()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreatePlateauPolicy(baseXp: BaseXp);
        DateOnly lastDay = new(2024, 1, 2);
        StreakState prior = new(2, 2, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(1);
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.Equal(3, result.State.CurrentStreak);
        Assert.Equal(3, result.State.LongestStreak);
        Assert.Equal(3, result.EffectiveStreakDay);
        Assert.Equal(240, result.XpAwarded); // base 100 * 1.2 curve * 2.0 plateau
    }

    [Fact]
    public void Evaluate_StaysAtPlateauAfterCap()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreatePlateauPolicy(baseXp: BaseXp);
        DateOnly lastDay = new(2024, 1, 3);
        StreakState prior = new(3, 3, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(1);
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.Equal(3, result.State.CurrentStreak);
        Assert.Equal(3, result.EffectiveStreakDay);
        Assert.Equal(240, result.XpAwarded);
    }

    [Fact]
    public void Evaluate_MissWithinGrace_ContinuesStreak()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreatePlateauPolicy(baseXp: BaseXp);
        DateOnly lastDay = new(2024, 1, 2);
        StreakState prior = new(2, 2, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(2); // one missed day
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.True(result.GraceApplied);
        Assert.Equal(1, result.State.GraceUsed);
        Assert.Equal(3, result.State.CurrentStreak);
        Assert.Equal(240, result.XpAwarded);
    }

    [Fact]
    public void Evaluate_MissBeyondGrace_ResetsStreak()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreatePlateauPolicy(baseXp: BaseXp);
        DateOnly lastDay = new(2024, 1, 2);
        StreakState prior = new(2, 2, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(3); // two missed days, beyond grace allowance
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.False(result.GraceApplied);
        Assert.Equal(0, result.State.GraceUsed);
        Assert.Equal(1, result.State.CurrentStreak);
        Assert.Equal(BaseXp, result.XpAwarded);
    }
}
