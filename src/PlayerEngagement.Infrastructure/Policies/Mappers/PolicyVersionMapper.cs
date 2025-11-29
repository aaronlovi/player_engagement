using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Shared.Json;
using PlayerEngagement.Shared.Validation;

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

        IReadOnlyDictionary<string, object?> parameters = JsonObjectParser.ParseObject(parametersJson);

        return type switch {
            StreakModelType.PlateauCap => MapPlateauCap(parameters),
            StreakModelType.WeeklyCycleReset => new WeeklyCycleResetStreakModel(),
            StreakModelType.DecayCurve => MapDecayCurve(parameters),
            StreakModelType.TieredSeasonalReset => MapTieredSeasonalReset(parameters),
            StreakModelType.MilestoneMetaReward => MapMilestoneMetaReward(parameters),
            _ => new RawStreakModelDefinition(type, parameters)
        };
    }

    private static PlateauCapStreakModel MapPlateauCap(IReadOnlyDictionary<string, object?> parameters) {
        int plateauDay = ParameterReader.RequireInt(parameters, "plateauDay", "plateau_day");
        decimal plateauMultiplier = ParameterReader.RequireDecimal(parameters, "plateauMultiplier", "plateau_multiplier");
        return new PlateauCapStreakModel(plateauDay, plateauMultiplier);
    }

    private static DecayCurveStreakModel MapDecayCurve(IReadOnlyDictionary<string, object?> parameters) {
        decimal decayPercent = ParameterReader.RequireDecimal(parameters, "decayPercent", "decay_percent", "decayRate", "decay_rate");
        int graceDay = ParameterReader.RequireInt(parameters, "graceDay", "grace_day");
        return new DecayCurveStreakModel(decayPercent, graceDay);
    }

    private static TieredSeasonalResetStreakModel MapTieredSeasonalReset(IReadOnlyDictionary<string, object?> parameters) {
        IReadOnlyList<object?> tiers = ParameterReader.RequireList(parameters, "tiers");
        List<TieredSeasonalResetTierDefinition> parsed = new(tiers.Count);

        foreach (object? tierObj in tiers) {
            IReadOnlyDictionary<string, object?> tierDict = ParameterReader.RequireDictionary(tierObj, "tiers[]");
            int startDay = ParameterReader.RequireInt(tierDict, "startDay", "start_day");
            int endDay = ParameterReader.RequireInt(tierDict, "endDay", "end_day");
            decimal bonusMultiplier = ParameterReader.RequireDecimal(tierDict, "bonusMultiplier", "bonus_multiplier");
            parsed.Add(new TieredSeasonalResetTierDefinition(startDay, endDay, bonusMultiplier));
        }

        return new TieredSeasonalResetStreakModel(parsed);
    }

    private static MilestoneMetaRewardStreakModel MapMilestoneMetaReward(IReadOnlyDictionary<string, object?> parameters) {
        IReadOnlyList<object?> milestones = ParameterReader.RequireList(parameters, "milestones");
        List<MilestoneMetaRewardMilestone> parsed = new(milestones.Count);

        foreach (object? milestoneObj in milestones) {
            IReadOnlyDictionary<string, object?> milestoneDict = ParameterReader.RequireDictionary(milestoneObj, "milestones[]");
            int day = ParameterReader.RequireInt(milestoneDict, "day");
            string rewardType = ParameterReader.RequireString(milestoneDict, "rewardType", "reward_type");
            string rewardValue = ParameterReader.RequireString(milestoneDict, "rewardValue", "reward_value");
            parsed.Add(new MilestoneMetaRewardMilestone(day, rewardType, rewardValue));
        }

        return new MilestoneMetaRewardStreakModel(parsed);
    }
}
