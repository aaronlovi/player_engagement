using System;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using Xunit;

namespace PlayerEngagement.Domain.Tests.Policies.DailyLoginBonus.XPGrant;

public sealed class StreakEngineTieredSeasonalTests
{
    private const int BaseXp = 100;

    [Fact]
    public void Evaluate_AppliesTierMultiplier()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreateTieredSeasonalPolicy(
            baseXp: BaseXp,
            streakCurve: new[]
            {
                new StreakCurveEntry(0, 1m, 0, false),
                new StreakCurveEntry(1, 1m, 0, false),
                new StreakCurveEntry(2, 1m, 0, false),
                new StreakCurveEntry(3, 1m, 0, false),
                new StreakCurveEntry(4, 1m, 0, false)
            });
        DateOnly lastDay = new(2024, 1, 1);
        StreakState prior = new(3, 3, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = lastDay.AddDays(1); // move to day 4 -> second tier (1.5x)
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.Equal(4, result.State.CurrentStreak);
        Assert.Equal(4, result.State.LongestStreak);
        Assert.Equal(1.5m, result.EffectiveMultiplier); // tier multiplier applied on top of curve 1.0
        Assert.Equal(150, result.XpAwarded);
    }

    [Fact]
    public void Evaluate_SeasonEnd_ResetsStreak()
    {
        StreakEngine engine = new();
        PolicyDocument policy = PolicyDocumentFactory.CreateTieredSeasonalPolicy(
            baseXp: BaseXp,
            streakCurve: new[]
            {
                new StreakCurveEntry(0, 1m, 0, false)
            });
        DateOnly seasonEnd = new(2024, 1, 10);
        SeasonBoundaryInfo season = new(seasonEnd.AddDays(-5), seasonEnd);

        DateOnly lastDay = seasonEnd;
        StreakState prior = new(5, 5, 0, lastDay, StreakModelRuntimeState.Empty);

        DateOnly rewardDay = seasonEnd.AddDays(1); // first claim after season end
        StreakTransitionContext context = new(policy, prior, rewardDay, DateTime.UnixEpoch, season);

        StreakTransitionResult result = engine.Evaluate(context);

        Assert.Equal(1, result.State.CurrentStreak);
        Assert.Equal(5, result.State.LongestStreak); // longest preserved
        Assert.Equal(1m, result.EffectiveMultiplier); // curve day1
        Assert.Equal(100, result.XpAwarded);
    }
}
