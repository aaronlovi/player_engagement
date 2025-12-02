using System;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Policies.Mappers.DailyLoginBonus.XPGrant;
using Xunit;

namespace PlayerEngagement.Infrastructure.Tests.Policies.Mappers;

public sealed class PolicyVersionMapperTests {
    [Fact]
    public void ToDomain_FromPolicyVersionDto_MapsCoreFields() {
        PolicyVersionDTO dto = CreateVersionDto(status: "Published", anchorStrategy: "FIXED_UTC");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        Assert.Equal(dto.PolicyVersion, result.PolicyVersion);
        Assert.Equal(PolicyVersionStatus.Published, result.Status);
        Assert.Equal(50, result.BaseXpAmount);
        Assert.Equal("XP", result.Currency);
        Assert.Equal(TimeSpan.FromMinutes(dto.ClaimWindowStartMinutes), result.ClaimWindowStartOffset);
        Assert.Equal(TimeSpan.FromHours(dto.ClaimWindowDurationHours), result.ClaimWindowDuration);
        Assert.Equal(AnchorStrategy.FixedUtc, result.AnchorStrategy);
        Assert.Equal("default", result.Preview.DefaultSegment);
        Assert.Equal(dto.EffectiveAt, result.EffectiveAt);
        MilestoneMetaRewardStreakModel milestoneModel = Assert.IsType<MilestoneMetaRewardStreakModel>(result.StreakModel);
        Assert.NotEmpty(milestoneModel.Milestones);
    }

    [Fact]
    public void ToDomain_FromActiveDto_ProvidesPreviewDefaults() {
        ActivePolicyDTO dto = CreateActiveDto(previewSegment: "", modelType: "DECAY_CURVE");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        Assert.Null(result.Preview.DefaultSegment);
        DecayCurveStreakModel model = Assert.IsType<DecayCurveStreakModel>(result.StreakModel);
        Assert.Equal(0.2m, model.DecayPercent);
        Assert.Equal(1, model.GraceDay);
    }

    [Fact]
    public void ToDomain_GracePolicyReflectsDtoValues() {
        PolicyVersionDTO dto = CreateVersionDto(graceAllowed: 2, graceWindow: 5);

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        Assert.Equal(2, result.GracePolicy.AllowedMisses);
        Assert.Equal(5, result.GracePolicy.WindowDays);
    }

    [Fact]
    public void ToDomain_PlateauCapStreakModel_MapsTypedDefinition() {
        PolicyVersionDTO dto = CreateVersionDto(
            modelType: "PLATEAU_CAP",
            modelParameters: "{\"plateauDay\":5,\"plateauMultiplier\":1.5}");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        PlateauCapStreakModel model = Assert.IsType<PlateauCapStreakModel>(result.StreakModel);
        Assert.Equal(5, model.PlateauDay);
        Assert.Equal(1.5m, model.PlateauMultiplier);
    }

    [Fact]
    public void ToDomain_PlateauCapMissingFields_Throws() {
        PolicyVersionDTO dto = CreateVersionDto(
            modelType: "PLATEAU_CAP",
            modelParameters: "{\"plateauMultiplier\":1.5}");

        _ = Assert.Throws<InvalidOperationException>(() => PolicyVersionMapper.ToDomain(dto));
    }

    [Fact]
    public void ToDomain_WeeklyCycleReset_MapsTypedDefinition() {
        PolicyVersionDTO dto = CreateVersionDto(
            modelType: "WEEKLY_CYCLE_RESET",
            modelParameters: "{}");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        _ = Assert.IsType<WeeklyCycleResetStreakModel>(result.StreakModel);
        Assert.Equal(WeeklyCycleResetStreakModel.CycleLength, WeeklyCycleResetStreakModel.CycleLength);
    }

    [Fact]
    public void ToDomain_DecayCurve_MapsTypedDefinition() {
        PolicyVersionDTO dto = CreateVersionDto(
            modelType: "DECAY_CURVE",
            modelParameters: "{\"decayPercent\":0.3,\"graceDay\":2}");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        DecayCurveStreakModel model = Assert.IsType<DecayCurveStreakModel>(result.StreakModel);
        Assert.Equal(0.3m, model.DecayPercent);
        Assert.Equal(2, model.GraceDay);
    }

    [Fact]
    public void ToDomain_TieredSeasonalReset_MapsTypedDefinition() {
        PolicyVersionDTO dto = CreateVersionDto(
            modelType: "TIERED_SEASONAL_RESET",
            modelParameters: "{\"tiers\":[{\"startDay\":1,\"endDay\":3,\"bonusMultiplier\":1.1},{\"startDay\":5,\"endDay\":7,\"bonusMultiplier\":1.2}]}");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        TieredSeasonalResetStreakModel model = Assert.IsType<TieredSeasonalResetStreakModel>(result.StreakModel);
        Assert.Equal(2, model.Tiers.Count);
        Assert.Equal(1, model.Tiers[0].StartDay);
        Assert.Equal(7, model.Tiers[1].EndDay);
    }

    [Fact]
    public void ToDomain_MilestoneMetaReward_MapsTypedDefinition() {
        PolicyVersionDTO dto = CreateVersionDto(
            modelType: "MILESTONE_META_REWARD",
            modelParameters: "{\"milestones\":[{\"day\":3,\"rewardType\":\"badge\",\"rewardValue\":\"bronze\"}]}");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        MilestoneMetaRewardStreakModel model = Assert.IsType<MilestoneMetaRewardStreakModel>(result.StreakModel);
        _ = Assert.Single(model.Milestones);
        Assert.Equal(3, model.Milestones[0].Day);
        Assert.Equal("badge", model.Milestones[0].RewardType);
        Assert.Equal("bronze", model.Milestones[0].RewardValue);
    }

    private static PolicyVersionDTO CreateVersionDto(
        string status = "Published",
        string anchorStrategy = "ANCHOR_TIMEZONE",
        int graceAllowed = 1,
        int graceWindow = 3,
        string modelType = "MILESTONE_META_REWARD",
        string modelParameters = "{\"milestones\":[{\"day\":7,\"rewardType\":\"badge\",\"rewardValue\":\"bronze\"}]}") =>
        new(
            1,
            "daily-login",
            "Daily Login",
            "desc",
            2,
            status,
            50,
            "XP",
            30,
            4,
            anchorStrategy,
            graceAllowed,
            graceWindow,
            modelType,
            modelParameters,
            10,
            "default",
            "{}",
            DateTime.UtcNow,
            null,
            DateTime.UtcNow.AddDays(-2),
            "agent",
            DateTime.UtcNow.AddDays(-1));

    private static ActivePolicyDTO CreateActiveDto(string previewSegment, string modelType) =>
        new(
            1,
            "daily-login",
            "Daily Login",
            "desc",
            1,
            "Draft",
            10,
            "XP",
            45,
            3,
            "SERVER_LOCAL",
            1,
            2,
            modelType,
            "{\"decay_rate\":0.2,\"grace_day\":1}",
            5,
            previewSegment,
            "{}",
            DateTime.UtcNow,
            null,
            DateTime.UtcNow.AddDays(-2),
            "agent",
            null);
}
