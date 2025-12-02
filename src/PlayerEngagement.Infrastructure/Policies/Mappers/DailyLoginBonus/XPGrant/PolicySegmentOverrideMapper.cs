using System;
using System.Collections.Generic;
using PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;

namespace PlayerEngagement.Infrastructure.Policies.Mappers.DailyLoginBonus.XPGrant;

internal static class PolicySegmentOverrideMapper {
    internal static IReadOnlyDictionary<string, long> ToDictionary(IEnumerable<PolicySegmentOverrideDTO> overrides) {
        ArgumentNullException.ThrowIfNull(overrides);

        Dictionary<string, long> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (PolicySegmentOverrideDTO dto in overrides) {
            if (dto.IsEmpty)
                continue;
            result[dto.SegmentKey] = dto.TargetPolicyVersion;
        }

        return result;
    }
}
