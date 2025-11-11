using System;
using System.Collections.Generic;
using System.Text.Json;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Policies.Mappers;

internal static class PolicyVersionMapper {
    internal static PolicyVersionDocument ToDomain(PolicyVersionDTO dto) {
        ArgumentNullException.ThrowIfNull(dto);

        PolicyVersionStatus status = MapStatus(dto.Status);
        AnchorStrategy anchor = MapAnchor(dto.AnchorStrategy);
        StreakModelDefinition streak = MapStreak(dto.StreakModelType, dto.StreakModelParameters);
        PreviewSettings preview = new(dto.PreviewSampleWindowDays, string.IsNullOrWhiteSpace(dto.PreviewDefaultSegment) ? null : dto.PreviewDefaultSegment);

        return new PolicyVersionDocument(
            dto.PolicyVersion,
            status,
            dto.BaseXpAmount,
            dto.Currency,
            TimeSpan.FromMinutes(dto.ClaimWindowStartMinutes),
            TimeSpan.FromHours(dto.ClaimWindowDurationHours),
            anchor,
            new GracePolicyDefinition(dto.GraceAllowedMisses, dto.GraceWindowDays),
            streak,
            preview,
            dto.EffectiveAt,
            dto.SupersededAt,
            dto.CreatedAt,
            dto.CreatedBy,
            dto.PublishedAt);
    }

    internal static PolicyVersionDocument ToDomain(ActivePolicyDTO dto) {
        ArgumentNullException.ThrowIfNull(dto);

        PolicyVersionStatus status = MapStatus(dto.Status);
        AnchorStrategy anchor = MapAnchor(dto.AnchorStrategy);
        StreakModelDefinition streak = MapStreak(dto.StreakModelType, dto.StreakModelParameters);
        PreviewSettings preview = new(dto.PreviewSampleWindowDays, string.IsNullOrWhiteSpace(dto.PreviewDefaultSegment) ? null : dto.PreviewDefaultSegment);

        return new PolicyVersionDocument(
            dto.PolicyVersion,
            status,
            dto.BaseXpAmount,
            dto.Currency,
            TimeSpan.FromMinutes(dto.ClaimWindowStartMinutes),
            TimeSpan.FromHours(dto.ClaimWindowDurationHours),
            anchor,
            new GracePolicyDefinition(dto.GraceAllowedMisses, dto.GraceWindowDays),
            streak,
            preview,
            dto.EffectiveAt,
            dto.SupersededAt,
            dto.CreatedAt,
            dto.CreatedBy,
            dto.PublishedAt);
    }

    private static PolicyVersionStatus MapStatus(string status) {
        if (string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase))
            return PolicyVersionStatus.Published;
        if (string.Equals(status, "Archived", StringComparison.OrdinalIgnoreCase))
            return PolicyVersionStatus.Archived;
        return PolicyVersionStatus.Draft;
    }

    private static AnchorStrategy MapAnchor(string anchorStrategy) {
        if (string.Equals(anchorStrategy, "FIXED_UTC", StringComparison.OrdinalIgnoreCase))
            return AnchorStrategy.FixedUtc;
        if (string.Equals(anchorStrategy, "SERVER_LOCAL", StringComparison.OrdinalIgnoreCase))
            return AnchorStrategy.ServerLocal;
        return AnchorStrategy.AnchorTimezone;
    }

    private static StreakModelDefinition MapStreak(string modelType, string parametersJson) {
        StreakModelType type = modelType switch {
            string s when string.Equals(s, "PLATEAU_CAP", StringComparison.OrdinalIgnoreCase) => StreakModelType.PlateauCap,
            string s when string.Equals(s, "WEEKLY_CYCLE_RESET", StringComparison.OrdinalIgnoreCase) => StreakModelType.WeeklyCycleReset,
            string s when string.Equals(s, "DECAY_CURVE", StringComparison.OrdinalIgnoreCase) => StreakModelType.DecayCurve,
            string s when string.Equals(s, "TIERED_SEASONAL_RESET", StringComparison.OrdinalIgnoreCase) => StreakModelType.TieredSeasonalReset,
            string s when string.Equals(s, "MILESTONE_META_REWARD", StringComparison.OrdinalIgnoreCase) => StreakModelType.MilestoneMetaReward,
            _ => StreakModelType.PlateauCap
        };

        IReadOnlyDictionary<string, object?> parameters = ParseParameters(parametersJson);
        return new StreakModelDefinition(type, parameters);
    }

    private static Dictionary<string, object?> ParseParameters(string json) {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            Dictionary<string, object?> result = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
                result[property.Name] = ConvertElement(property.Value);

            return result;
        } catch (JsonException) {
            return [];
        }
    }

    private static object? ConvertElement(JsonElement element) => element.ValueKind switch {
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDecimal(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Array => ConvertArray(element),
        JsonValueKind.Object => ConvertObject(element),
        _ => null
    };

    private static List<object?> ConvertArray(JsonElement element) {
        List<object?> items = new(element.GetArrayLength());
        foreach (JsonElement child in element.EnumerateArray())
            items.Add(ConvertElement(child));
        return items;
    }

    private static Dictionary<string, object?> ConvertObject(JsonElement element) {
        Dictionary<string, object?> nested = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in element.EnumerateObject())
            nested[property.Name] = ConvertElement(property.Value);
        return nested;
    }
}
