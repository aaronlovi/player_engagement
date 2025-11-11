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
    }

    [Fact]
    public void ToDomain_FromActiveDto_ProvidesPreviewDefaults() {
        ActivePolicyDTO dto = CreateActiveDto(previewSegment: "");

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        Assert.Null(result.Preview.DefaultSegment);
        Assert.Equal(StreakModelType.DecayCurve, result.StreakModel.Type);
        Assert.Contains("decay_rate", result.StreakModel.Parameters.Keys);
        Assert.Equal(0.2m, result.StreakModel.Parameters["decay_rate"]);
    }

    [Fact]
    public void ToDomain_GracePolicyReflectsDtoValues() {
        PolicyVersionDTO dto = CreateVersionDto(graceAllowed: 2, graceWindow: 5);

        PolicyVersionDocument result = PolicyVersionMapper.ToDomain(dto);

        Assert.Equal(2, result.GracePolicy.AllowedMisses);
        Assert.Equal(5, result.GracePolicy.WindowDays);
    }

    private static PolicyVersionDTO CreateVersionDto(
        string status = "Published",
        string anchorStrategy = "ANCHOR_TIMEZONE",
        int graceAllowed = 1,
        int graceWindow = 3) =>
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
            "MILESTONE_META_REWARD",
            "{\"milestones\":[{\"day\":7,\"bonus\":100}],\"decay_rate\":0.2}",
            10,
            "default",
            "{}",
            DateTime.UtcNow,
            null,
            DateTime.UtcNow.AddDays(-2),
            "agent",
            DateTime.UtcNow.AddDays(-1));

    private static ActivePolicyDTO CreateActiveDto(string previewSegment) =>
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
            "DECAY_CURVE",
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
