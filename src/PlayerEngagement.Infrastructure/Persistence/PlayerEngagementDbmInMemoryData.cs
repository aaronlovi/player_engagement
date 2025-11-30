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
                string.Equals(stored.Status, "Published", StringComparison.OrdinalIgnoreCase) &&
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

    internal static Result<PolicyVersionDTO> PublishPolicyVersion(
        string policyKey,
        long policyVersion,
        DateTime publishedAt,
        DateTime? effectiveAt,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides) {

        lock (Sync) {
            if (!PolicyVersions.TryGetValue((policyKey, policyVersion), out PolicyVersionDTO? existing) || existing.IsEmpty)
                return Result<PolicyVersionDTO>.Failure(ErrorCodes.NotFound, $"Policy '{policyKey}' version '{policyVersion}' not found.");

            if (string.Equals(existing.Status, "Published", StringComparison.OrdinalIgnoreCase))
                return Result<PolicyVersionDTO>.Failure(ErrorCodes.Duplicate, $"Policy '{policyKey}' version '{policyVersion}' is already published.");

            ArchiveCurrentPublished(policyKey, effectiveAt ?? existing.EffectiveAt ?? publishedAt);

            PolicyVersionDTO published = existing with {
                Status = "Published",
                EffectiveAt = effectiveAt ?? existing.EffectiveAt ?? publishedAt,
                SupersededAt = null,
                PublishedAt = publishedAt
            };

            PolicyVersions[(policyKey, policyVersion)] = published;
            ActivePolicies[policyKey] = ToActive(published);

            if (overrides.Count > 0)
                SegmentOverrides[policyKey] = [.. overrides];

            return Result<PolicyVersionDTO>.Success(published);
        }
    }

    internal static Result<PolicyVersionDTO> RetirePolicyVersion(string policyKey, long policyVersion, DateTime retiredAt) {
        lock (Sync) {
            if (!PolicyVersions.TryGetValue((policyKey, policyVersion), out PolicyVersionDTO? existing) || existing.IsEmpty)
                return Result<PolicyVersionDTO>.Failure(ErrorCodes.NotFound, $"Policy '{policyKey}' version '{policyVersion}' not found.");

            if (!string.Equals(existing.Status, "Published", StringComparison.OrdinalIgnoreCase))
                return Result<PolicyVersionDTO>.Failure(ErrorCodes.Duplicate, $"Policy '{policyKey}' version '{policyVersion}' is not published.");

            PolicyVersionDTO retired = existing with {
                Status = "Archived",
                SupersededAt = retiredAt
            };

            PolicyVersions[(policyKey, policyVersion)] = retired;
            _ = ActivePolicies.Remove(policyKey);
            return Result<PolicyVersionDTO>.Success(retired);
        }
    }

    internal static Result ReplaceStreakCurve(string policyKey, long policyVersion, IReadOnlyList<PolicyStreakCurveEntryDTO> entries) {
        lock (Sync) {
            StreakCurves[(policyKey, policyVersion)] = NormalizeStreakEntries(policyKey, policyVersion, entries);
            return Result.Success;
        }
    }

    internal static Result ReplaceSeasonalBoosts(string policyKey, long policyVersion, IReadOnlyList<PolicySeasonalBoostDTO> boosts) {
        lock (Sync) {
            SeasonalBoosts[(policyKey, policyVersion)] = NormalizeSeasonalBoosts(policyKey, policyVersion, boosts);
            return Result.Success;
        }
    }

    internal static Result UpsertSegmentOverrides(string policyKey, IReadOnlyList<PolicySegmentOverrideDTO> overrides) {
        lock (Sync) {
            SegmentOverrides[policyKey] = [.. overrides];
            return Result.Success;
        }
    }

    internal static List<PolicyVersionDTO> ListPolicyVersions(string policyKey, string? status, DateTime? effectiveBefore, int? limit) {
        lock (Sync) {
            List<PolicyVersionDTO> versions = [];
            foreach (KeyValuePair<(string PolicyKey, long PolicyVersion), PolicyVersionDTO> pair in PolicyVersions) {
                if (pair.Value.IsEmpty)
                    continue;

                if (!string.Equals(pair.Key.PolicyKey, policyKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrWhiteSpace(status) &&
                    !string.Equals(pair.Value.Status, status!, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                if (effectiveBefore.HasValue &&
                    pair.Value.EffectiveAt.HasValue &&
                    pair.Value.EffectiveAt.Value > effectiveBefore.Value) {
                    continue;
                }

                versions.Add(pair.Value);
            }

            versions.Sort(static (a, b) => b.PolicyVersion.CompareTo(a.PolicyVersion));

            if (limit.HasValue && limit.Value > 0 && versions.Count > limit.Value) {
                while (versions.Count > limit.Value)
                    versions.RemoveAt(versions.Count - 1);
            }

            return versions;
        }
    }

    private static void ArchiveCurrentPublished(string policyKey, DateTime? supersededAt) {
        foreach (KeyValuePair<(string PolicyKey, long PolicyVersion), PolicyVersionDTO> pair in PolicyVersions) {
            if (!string.Equals(pair.Key.PolicyKey, policyKey, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.Equals(pair.Value.Status, "Published", StringComparison.OrdinalIgnoreCase))
                continue;

            PolicyVersionDTO archived = pair.Value with {
                Status = "Archived",
                SupersededAt = supersededAt ?? pair.Value.SupersededAt
            };

            PolicyVersions[pair.Key] = archived;
        }
    }

    private static ActivePolicyDTO ToActive(PolicyVersionDTO source) => new(
        source.PolicyId,
        source.PolicyKey,
        source.DisplayName,
        source.Description,
        source.PolicyVersion,
        source.Status,
        source.BaseXpAmount,
        source.Currency,
        source.ClaimWindowStartMinutes,
        source.ClaimWindowDurationHours,
        source.AnchorStrategy,
        source.GraceAllowedMisses,
        source.GraceWindowDays,
        source.StreakModelType,
        source.StreakModelParameters,
        source.PreviewSampleWindowDays,
        source.PreviewDefaultSegment,
        source.SeasonalMetadata,
        source.EffectiveAt,
        source.SupersededAt,
        source.CreatedAt,
        source.CreatedBy,
        source.PublishedAt);

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
