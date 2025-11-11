using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Policies.Mappers;

internal static class PolicyDocumentMapper {
    internal static PolicyDocument ToDomain(
        PolicyVersionDTO versionDto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streakCurveDtos,
        IReadOnlyList<PolicySeasonalBoostDTO> seasonalBoostDtos) {

        PolicyVersionDocument version = PolicyVersionMapper.ToDomain(versionDto);
        List<StreakCurveEntry> streakCurve = MapStreakCurve(streakCurveDtos);
        List<SeasonalBoost> seasonalBoosts = MapSeasonalBoosts(seasonalBoostDtos);

        return new PolicyDocument(
            versionDto.PolicyKey,
            versionDto.DisplayName,
            versionDto.Description,
            version,
            streakCurve,
            seasonalBoosts);
    }

    internal static PolicyDocument ToDomain(
        ActivePolicyDTO activeDto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streakCurveDtos,
        IReadOnlyList<PolicySeasonalBoostDTO> seasonalBoostDtos) {

        PolicyVersionDocument version = PolicyVersionMapper.ToDomain(activeDto);
        List<StreakCurveEntry> streakCurve = MapStreakCurve(streakCurveDtos);
        List<SeasonalBoost> seasonalBoosts = MapSeasonalBoosts(seasonalBoostDtos);

        return new PolicyDocument(
            activeDto.PolicyKey,
            activeDto.DisplayName,
            activeDto.Description,
            version,
            streakCurve,
            seasonalBoosts);
    }

    private static List<StreakCurveEntry> MapStreakCurve(IReadOnlyList<PolicyStreakCurveEntryDTO> dtos) {
        List<StreakCurveEntry> entries = new(dtos.Count);
        for (int i = 0; i < dtos.Count; i++) {
            PolicyStreakCurveEntryDTO dto = dtos[i];
            entries.Add(new StreakCurveEntry(dto.DayIndex, dto.Multiplier, dto.AdditiveBonusXp, dto.CapNextDay));
        }

        return entries;
    }

    private static List<SeasonalBoost> MapSeasonalBoosts(IReadOnlyList<PolicySeasonalBoostDTO> dtos) {
        List<SeasonalBoost> boosts = new(dtos.Count);
        for (int i = 0; i < dtos.Count; i++) {
            PolicySeasonalBoostDTO dto = dtos[i];
            DateTime startUtc = dto.StartUtc;
            DateTime endUtc = dto.EndUtc;
            boosts.Add(new SeasonalBoost(dto.BoostId, dto.Label, dto.Multiplier, startUtc, endUtc));
        }

        return boosts;
    }
}
