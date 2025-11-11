using System;
using System.Collections.Generic;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence;

internal class PlayerEngagementDbmInMemoryData {
    private static readonly Dictionary<string, ActivePolicyDTO> ActivePolicies = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<(string PolicyKey, int PolicyVersion), PolicyVersionDTO> PolicyVersions = [];
    private static readonly Dictionary<(string PolicyKey, int PolicyVersion), List<PolicyStreakCurveEntryDTO>> StreakCurves = [];
    private static readonly Dictionary<(string PolicyKey, int PolicyVersion), List<PolicySeasonalBoostDTO>> SeasonalBoosts = [];
    private static readonly Dictionary<string, List<PolicySegmentOverrideDTO>> SegmentOverrides = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Sync = new();

    internal static bool TryGetActivePolicy(string policyKey, DateTime utcNow, out ActivePolicyDTO dto) {
        lock (Sync) {
            if (ActivePolicies.TryGetValue(policyKey, out ActivePolicyDTO? stored) &&
                !stored.IsEmpty &&
                (!stored.EffectiveAt.HasValue || stored.EffectiveAt.Value <= utcNow)) {
                dto = stored;
                return true;
            }

            dto = ActivePolicyDTO.Empty;
            return false;
        }
    }

    internal static bool TryGetPolicyVersion(string policyKey, int policyVersion, out PolicyVersionDTO dto) {
        lock (Sync) {
            if (PolicyVersions.TryGetValue((policyKey, policyVersion), out PolicyVersionDTO? stored) && !stored.IsEmpty) {
                dto = stored;
                return true;
            }

            dto = PolicyVersionDTO.Empty;
            return false;
        }
    }

    internal static List<PolicyVersionDTO> ListPublishedPolicies(DateTime utcNow) {
        lock (Sync) {
            List<PolicyVersionDTO> results = [];
            foreach (PolicyVersionDTO version in PolicyVersions.Values) {
                if (version.IsEmpty)
                    continue;

                if (!string.Equals(version.Status, "Published", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (version.EffectiveAt.HasValue && version.EffectiveAt.Value > utcNow)
                    continue;

                results.Add(version);
            }

            return results;
        }
    }

    internal static List<PolicyStreakCurveEntryDTO> GetStreakCurve(string policyKey, int policyVersion) {
        lock (Sync) {
            if (!StreakCurves.TryGetValue((policyKey, policyVersion), out List<PolicyStreakCurveEntryDTO>? stored))
                return [];

            return [.. stored];
        }
    }

    internal static List<PolicySeasonalBoostDTO> GetSeasonalBoosts(string policyKey, int policyVersion) {
        lock (Sync) {
            if (!SeasonalBoosts.TryGetValue((policyKey, policyVersion), out List<PolicySeasonalBoostDTO>? stored))
                return [];

            return [.. stored];
        }
    }

    internal static List<PolicySegmentOverrideDTO> GetSegmentOverrides(string policyKey) {
        lock (Sync) {
            if (!SegmentOverrides.TryGetValue(policyKey, out List<PolicySegmentOverrideDTO>? stored))
                return [];

            return [.. stored];
        }
    }

    // Helper mutators for tests or seeding
    internal static void UpsertActivePolicy(ActivePolicyDTO dto) {
        lock (Sync)
            ActivePolicies[dto.PolicyKey] = dto;
    }

    internal static void UpsertPolicyVersion(PolicyVersionDTO dto) {
        lock (Sync)
            PolicyVersions[(dto.PolicyKey, dto.PolicyVersion)] = dto;
    }

    internal static void SetStreakCurve(string policyKey, int policyVersion, IEnumerable<PolicyStreakCurveEntryDTO> entries) {
        lock (Sync)
            StreakCurves[(policyKey, policyVersion)] = [.. entries];
    }

    internal static void SetSeasonalBoosts(string policyKey, int policyVersion, IEnumerable<PolicySeasonalBoostDTO> boosts) {
        lock (Sync)
            SeasonalBoosts[(policyKey, policyVersion)] = [.. boosts];
    }

    internal static void SetSegmentOverrides(string policyKey, IEnumerable<PolicySegmentOverrideDTO> overrides) {
        lock (Sync)
            SegmentOverrides[policyKey] = [.. overrides];
    }
}
