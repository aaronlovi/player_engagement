using System;

namespace PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

/// <summary>
/// Projection of xp_policy_versions joined to xp_policies for arbitrary version lookups.
/// </summary>
public sealed record PolicyVersionDTO(
    long PolicyId,
    string PolicyKey,
    string DisplayName,
    string Description,
    int PolicyVersion,
    string Status,
    int BaseXpAmount,
    string Currency,
    int ClaimWindowStartMinutes,
    int ClaimWindowDurationHours,
    string AnchorStrategy,
    int GraceAllowedMisses,
    int GraceWindowDays,
    string StreakModelType,
    string StreakModelParameters,
    int PreviewSampleWindowDays,
    string PreviewDefaultSegment,
    string SeasonalMetadata,
    DateTime? EffectiveAt,
    DateTime? SupersededAt,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? PublishedAt) {
    public static readonly PolicyVersionDTO Empty = new(
        0, string.Empty, string.Empty, string.Empty,
        0, string.Empty, 0, string.Empty,
        0, 0, string.Empty, 0, 0,
        string.Empty, string.Empty, 0, string.Empty,
        string.Empty, null, null, DateTime.MinValue, string.Empty, null);

    public bool IsEmpty => PolicyId == 0;
}
