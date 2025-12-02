using System;

namespace PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;

/// <summary>
/// Projection of the active policy join (xp_policies + xp_policy_versions) as returned by the active-policy statement.
/// </summary>
public sealed record ActivePolicyDTO(
    long PolicyId,
    string PolicyKey,
    string DisplayName,
    string Description,
    long PolicyVersion,
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
    public static readonly ActivePolicyDTO Empty = new(
        0, string.Empty, string.Empty, string.Empty,
        0, string.Empty, 0, string.Empty,
        0, 0, string.Empty, 0, 0,
        string.Empty, string.Empty, 0, string.Empty,
        string.Empty, null, null, DateTime.MinValue, string.Empty, null);

    public bool IsEmpty => PolicyId == 0;
}
