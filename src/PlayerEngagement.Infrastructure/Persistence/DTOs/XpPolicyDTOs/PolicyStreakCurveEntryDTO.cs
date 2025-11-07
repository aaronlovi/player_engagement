namespace PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

/// <summary>
/// DTO representing a single row from xp_policy_streak_curve.
/// </summary>
public sealed record PolicyStreakCurveEntryDTO(
    long StreakCurveId,
    string PolicyKey,
    int PolicyVersion,
    int DayIndex,
    decimal Multiplier,
    int AdditiveBonusXp,
    bool CapNextDay) {
    public static readonly PolicyStreakCurveEntryDTO Empty = new(
        0, string.Empty, 0, 0, 0m, 0, false);

    public bool IsEmpty => StreakCurveId == 0;
}
