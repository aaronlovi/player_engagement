using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Infrastructure.Policies.Mappers;
using PlayerEngagement.Infrastructure.Tests.TestUtilities;
using Xunit;

namespace PlayerEngagement.Infrastructure.Tests.Policies.Mappers;

public sealed class PolicyDocumentMapperTests {
    [Fact]
    public void ToDomain_FromVersionDto_ProducesPolicyDocument() {
        PolicyVersionDTO dto = PolicyDtoFactory.CreateVersion("daily-login", version: 3);
        List<PolicyStreakCurveEntryDTO> streak = [
            new(1, dto.PolicyKey, dto.PolicyVersion, 1, 1.0m, 0, false),
            new(2, dto.PolicyKey, dto.PolicyVersion, 2, 1.5m, 10, true)
        ];
        List<PolicySeasonalBoostDTO> boosts = [
            new(100, dto.PolicyKey, dto.PolicyVersion, "Spring Boost", 2.0m, DateTime.UtcNow, DateTime.UtcNow.AddDays(7))
        ];

        PolicyDocument document = PolicyDocumentMapper.ToDomain(dto, streak, boosts);

        Assert.Equal(dto.PolicyKey, document.PolicyKey);
        Assert.Equal(dto.DisplayName, document.DisplayName);
        Assert.Equal(dto.PolicyVersion, document.Version.PolicyVersion);
        _ = Assert.IsType<MilestoneMetaRewardStreakModel>(document.Version.StreakModel);
        Assert.Equal(2, document.StreakCurve.Count);
        _ = Assert.Single(document.SeasonalBoosts, b => b.Label == "Spring Boost");
    }

    [Fact]
    public void ToDomain_FromActiveDto_UsesActiveDisplayInfo() {
        ActivePolicyDTO dto = PolicyDtoFactory.CreateActive("daily-login", version: 4);

        PolicyDocument document = PolicyDocumentMapper.ToDomain(dto, [], []);

        Assert.Equal(dto.PolicyKey, document.PolicyKey);
        Assert.Equal(dto.DisplayName, document.DisplayName);
        Assert.Empty(document.StreakCurve);
        Assert.Empty(document.SeasonalBoosts);
        _ = Assert.IsType<DecayCurveStreakModel>(document.Version.StreakModel);
    }

    [Fact]
    public void ToDomain_WithPlateauModel_MapsStreakModel() {
        PolicyVersionDTO dto = PolicyDtoFactory.CreateVersion(
            "daily-login",
            modelType: "PLATEAU_CAP",
            modelParameters: "{\"plateauDay\":3,\"plateauMultiplier\":2.0}");

        PolicyDocument document = PolicyDocumentMapper.ToDomain(dto, [], []);

        _ = Assert.IsType<PlateauCapStreakModel>(document.Version.StreakModel);
    }

    [Fact]
    public void ToDomain_WithWeeklyCycleResetModel_MapsStreakModel() {
        PolicyVersionDTO dto = PolicyDtoFactory.CreateVersion(
            "daily-login",
            modelType: "WEEKLY_CYCLE_RESET",
            modelParameters: "{}");

        PolicyDocument document = PolicyDocumentMapper.ToDomain(dto, [], []);

        _ = Assert.IsType<WeeklyCycleResetStreakModel>(document.Version.StreakModel);
    }

    [Fact]
    public void ToDomain_WithTieredSeasonalResetModel_MapsStreakModel() {
        PolicyVersionDTO dto = PolicyDtoFactory.CreateVersion(
            "daily-login",
            modelType: "TIERED_SEASONAL_RESET",
            modelParameters: "{\"tiers\":[{\"startDay\":1,\"endDay\":3,\"bonusMultiplier\":1.1},{\"startDay\":5,\"endDay\":7,\"bonusMultiplier\":1.2}]}");

        PolicyDocument document = PolicyDocumentMapper.ToDomain(dto, [], []);

        _ = Assert.IsType<TieredSeasonalResetStreakModel>(document.Version.StreakModel);
    }
}
