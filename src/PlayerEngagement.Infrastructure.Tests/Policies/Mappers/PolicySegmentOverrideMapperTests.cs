using System;
using System.Collections.Generic;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Infrastructure.Policies.Mappers;
using Xunit;

namespace PlayerEngagement.Infrastructure.Tests.Policies.Mappers;

public sealed class PolicySegmentOverrideMapperTests {
    [Fact]
    public void ToDictionary_IgnoresEmptyOverrides() {
        PolicySegmentOverrideDTO empty = PolicySegmentOverrideDTO.Empty;
        PolicySegmentOverrideDTO valid = new(1, "vip", "daily-login", 3, DateTime.UtcNow, "agent");

        IReadOnlyDictionary<string, int> result = PolicySegmentOverrideMapper.ToDictionary(new[] { empty, valid });

        _ = Assert.Single(result);
        Assert.Equal(3, result["VIP"]);
    }
}
