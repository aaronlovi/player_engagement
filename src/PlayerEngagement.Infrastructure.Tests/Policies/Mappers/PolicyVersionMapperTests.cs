using System;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Infrastructure.Policies.Mappers;
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
        RawStreakModelDefinition rawModel = Assert.IsType<RawStreakModelDefinition>(result.StreakModel);
        Assert.Equal(StreakModelType.MilestoneMetaReward, rawModel.Type);
    }

    [Fact]
    public void ToDomain_FromActiveDto_ProvidesPreviewDefaults() {
        ActivePolicyDTO dto = CreateActiveDto(previewSegment: "", modelType: "DECAY_CURVE");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        Assert.Null(result.Preview.DefaultSegment);
        RawStreakModelDefinition rawModel = Assert.IsType<RawStreakModelDefinition>(result.StreakModel);
        Assert.Equal(StreakModelType.DecayCurve, rawModel.Type);
        Assert.Contains("decay_rate", rawModel.Parameters.Keys);
        Assert.Equal(0.2m, rawModel.Parameters["decay_rate"]);
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

        Assert.Throws<InvalidOperationException>(() => PolicyVersionMapper.ToDomain(dto));
    }

    private static PolicyVersionDTO CreateVersionDto(
        string status = "Published",
        string anchorStrategy = "ANCHOR_TIMEZONE",
        int graceAllowed = 1,
        int graceWindow = 3,
        string modelType = "MILESTONE_META_REWARD",
        string modelParameters = "{\"milestones\":[{\"day\":7,\"bonus\":100}],\"decay_rate\":0.2}") =>
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
            "{\"decay_rate\":0.2}",
            5,
            previewSegment,
            "{}",
            DateTime.UtcNow,
            null,
            DateTime.UtcNow.AddDays(-2),
            "agent",
            null);
}
