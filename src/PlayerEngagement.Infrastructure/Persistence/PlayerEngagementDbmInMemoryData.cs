using System;
using System.Collections.Generic;
using System.Threading;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence;

internal class PlayerEngagementDbmInMemoryData {
    private static readonly Dictionary<string, ActivePolicyDTO> ActivePolicies = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, PolicyMetadata> Policies = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<(string PolicyKey, long PolicyVersion), PolicyVersionDTO> PolicyVersions = [];
    private static readonly Dictionary<(string PolicyKey, long PolicyVersion), List<PolicyStreakCurveEntryDTO>> StreakCurves = [];
    private static readonly Dictionary<(string PolicyKey, long PolicyVersion), List<PolicySeasonalBoostDTO>> SeasonalBoosts = [];
    private static readonly Dictionary<string, List<PolicySegmentOverrideDTO>> SegmentOverrides = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Sync = new();
    private static long _nextInternalId = 1;
    private static long NextInternalId() => Interlocked.Increment(ref _nextInternalId);

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

    internal static bool TryGetPolicyVersion(string policyKey, long policyVersion, out PolicyVersionDTO dto) {
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

    internal static List<PolicyStreakCurveEntryDTO> GetStreakCurve(string policyKey, long policyVersion) {
        lock (Sync) {
            if (!StreakCurves.TryGetValue((policyKey, policyVersion), out List<PolicyStreakCurveEntryDTO>? stored))
                return [];

            return [.. stored];
        }
    }

    internal static List<PolicySeasonalBoostDTO> GetSeasonalBoosts(string policyKey, long policyVersion) {
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
        lock (Sync) {
            Policies[dto.PolicyKey] = new PolicyMetadata(dto.PolicyId, dto.DisplayName, dto.Description, dto.CreatedAt, dto.CreatedBy);
            ActivePolicies[dto.PolicyKey] = dto;
        }
    }

    internal static void UpsertPolicyVersion(PolicyVersionDTO dto) {
        lock (Sync) {
            Policies[dto.PolicyKey] = new PolicyMetadata(dto.PolicyId, dto.DisplayName, dto.Description, dto.CreatedAt, dto.CreatedBy);
            PolicyVersions[(dto.PolicyKey, dto.PolicyVersion)] = dto;
        }
    }

    internal static void SetStreakCurve(string policyKey, long policyVersion, IEnumerable<PolicyStreakCurveEntryDTO> entries) {
        lock (Sync)
            StreakCurves[(policyKey, policyVersion)] = [.. entries];
    }

    internal static void SetSeasonalBoosts(string policyKey, long policyVersion, IEnumerable<PolicySeasonalBoostDTO> boosts) {
        lock (Sync)
            SeasonalBoosts[(policyKey, policyVersion)] = [.. boosts];
    }

    internal static void SetSegmentOverrides(string policyKey, IEnumerable<PolicySegmentOverrideDTO> overrides) {
        lock (Sync)
            SegmentOverrides[policyKey] = [.. overrides];
    }

    internal static Result<long> CreatePolicyDraft(
        PolicyVersionWriteDto dto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streak,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts) {

        lock (Sync) {
            if (PolicyVersions.ContainsKey((dto.PolicyKey, dto.PolicyVersion)))
                return Result<long>.Failure(ErrorCodes.Duplicate, $"Policy '{dto.PolicyKey}' version '{dto.PolicyVersion}' already exists.");

            PolicyMetadata metadata;
            if (!Policies.TryGetValue(dto.PolicyKey, out metadata!)) {
                if (!dto.PolicyId.HasValue)
                    return Result<long>.Failure(ErrorCodes.ValidationError, "policyId must be provided for new policies.");

                metadata = new PolicyMetadata(
                    dto.PolicyId.Value,
                    dto.DisplayName,
                    dto.Description ?? string.Empty,
                    dto.CreatedAt,
                    dto.CreatedBy);

                Policies[dto.PolicyKey] = metadata;
            } else {
                metadata = metadata with {
                    DisplayName = dto.DisplayName,
                    Description = dto.Description ?? metadata.Description
                };
                Policies[dto.PolicyKey] = metadata;
            }

            PolicyVersionDTO versionDto = new(
                metadata.PolicyId,
                dto.PolicyKey,
                dto.DisplayName,
                dto.Description ?? string.Empty,
                dto.PolicyVersion,
                "Draft",
                dto.BaseXpAmount,
                dto.Currency,
                dto.ClaimWindowStartMinutes,
                dto.ClaimWindowDurationHours,
                dto.AnchorStrategy,
                dto.GraceAllowedMisses,
                dto.GraceWindowDays,
                dto.StreakModelType,
                dto.StreakModelParameters,
                dto.PreviewSampleWindowDays,
                dto.PreviewDefaultSegment ?? string.Empty,
                dto.SeasonalMetadata,
                dto.EffectiveAt,
                null,
                dto.CreatedAt,
                dto.CreatedBy,
                null);

            PolicyVersions[(dto.PolicyKey, dto.PolicyVersion)] = versionDto;
            StreakCurves[(dto.PolicyKey, dto.PolicyVersion)] = NormalizeStreakEntries(dto.PolicyKey, dto.PolicyVersion, streak);
            SeasonalBoosts[(dto.PolicyKey, dto.PolicyVersion)] = NormalizeSeasonalBoosts(dto.PolicyKey, dto.PolicyVersion, boosts);

            return Result<long>.Success(dto.PolicyVersion);
        }
    }

    private static List<PolicyStreakCurveEntryDTO> NormalizeStreakEntries(string policyKey, long policyVersion, IReadOnlyList<PolicyStreakCurveEntryDTO> entries) {
        if (entries.Count == 0)
            return [];

        List<PolicyStreakCurveEntryDTO> normalized = new(entries.Count);
        foreach (PolicyStreakCurveEntryDTO entry in entries) {
            long id = entry.StreakCurveId != 0 ? entry.StreakCurveId : NextInternalId();
            normalized.Add(new PolicyStreakCurveEntryDTO(
                id,
                policyKey,
                policyVersion,
                entry.DayIndex,
                entry.Multiplier,
                entry.AdditiveBonusXp,
                entry.CapNextDay));
        }

        return normalized;
    }

    private static List<PolicySeasonalBoostDTO> NormalizeSeasonalBoosts(string policyKey, long policyVersion, IReadOnlyList<PolicySeasonalBoostDTO> boosts) {
        if (boosts.Count == 0)
            return [];

        List<PolicySeasonalBoostDTO> normalized = new(boosts.Count);
        foreach (PolicySeasonalBoostDTO boost in boosts) {
            long id = boost.BoostId != 0 ? boost.BoostId : NextInternalId();
            normalized.Add(new PolicySeasonalBoostDTO(
                id,
                policyKey,
                policyVersion,
                boost.Label,
                boost.Multiplier,
                boost.StartUtc,
                boost.EndUtc));
        }

        return normalized;
    }

    private sealed record PolicyMetadata(
        long PolicyId,
        string DisplayName,
        string Description,
        DateTime CreatedAt,
        string CreatedBy);
}
