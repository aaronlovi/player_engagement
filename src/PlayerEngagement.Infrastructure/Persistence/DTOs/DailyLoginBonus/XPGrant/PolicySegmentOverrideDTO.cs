using System;

namespace PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;

/// <summary>
/// DTO representing a row from daily_login_bonus_xp_policy_segment_overrides.
/// </summary>
public sealed record PolicySegmentOverrideDTO(
    long OverrideId,
    string SegmentKey,
    string PolicyKey,
    long TargetPolicyVersion,
    DateTime CreatedAt,
    string CreatedBy) {
    public static readonly PolicySegmentOverrideDTO Empty = new(
        0, string.Empty, string.Empty, 0, DateTime.MinValue, string.Empty);

    public bool IsEmpty => OverrideId == 0;
}
