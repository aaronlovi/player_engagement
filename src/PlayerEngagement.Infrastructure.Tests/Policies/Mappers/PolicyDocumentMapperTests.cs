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
    }
}
