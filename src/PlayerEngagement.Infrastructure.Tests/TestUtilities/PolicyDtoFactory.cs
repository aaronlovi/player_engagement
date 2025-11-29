using System;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Tests.TestUtilities;

internal static class PolicyDtoFactory {
    internal static ActivePolicyDTO CreateActive(
        string policyKey,
        long version = 1,
        DateTime? effectiveAt = null) =>
        new(
            10L,
            policyKey,
            $"{policyKey}-display",
            "desc",
            version,
            "Published",
            100,
            "XP",
            30,
            4,
            "ANCHOR_TIMEZONE",
            1,
            2,
            "PLATEAU_CAP",
            "{\"plateauDay\":7,\"plateauMultiplier\":1.0}",
            5,
            "default",
            "{}",
            effectiveAt ?? DateTime.UtcNow.AddHours(-1),
            null,
            DateTime.UtcNow.AddDays(-2),
            "agent",
            DateTime.UtcNow.AddDays(-1));

    internal static PolicyVersionDTO CreateVersion(
        string policyKey,
        long version = 1,
        string status = "Published",
        DateTime? effectiveAt = null) =>
        new(
            10L,
            policyKey,
            $"{policyKey}-display",
            "desc",
            version,
            status,
            100,
            "XP",
            30,
            4,
            "ANCHOR_TIMEZONE",
            1,
            2,
            "PLATEAU_CAP",
            "{\"plateauDay\":7,\"plateauMultiplier\":1.0}",
            5,
            "default",
            "{}",
            effectiveAt,
            null,
            DateTime.UtcNow.AddDays(-2),
            "agent",
            DateTime.UtcNow.AddDays(-1));
}
