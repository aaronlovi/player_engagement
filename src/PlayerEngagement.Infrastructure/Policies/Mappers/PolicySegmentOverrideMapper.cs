using System;
using System.Collections.Generic;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Policies.Mappers;

internal static class PolicySegmentOverrideMapper {
    internal static IReadOnlyDictionary<string, int> ToDictionary(IEnumerable<PolicySegmentOverrideDTO> overrides) {
        ArgumentNullException.ThrowIfNull(overrides);

        Dictionary<string, int> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (PolicySegmentOverrideDTO dto in overrides) {
            if (dto.IsEmpty)
                continue;
            result[dto.SegmentKey] = dto.TargetPolicyVersion;
        }

        return result;
    }
}
