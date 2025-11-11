namespace PlayerEngagement.Domain.Policies;

/// <summary>Represents a single streak curve point (day index, multiplier, bonuses).</summary>
/// <param name="DayIndex">Zero-based streak day index.</param>
/// <param name="Multiplier">Multiplier applied to the base XP on this day.</param>
/// <param name="AdditiveBonusXp">Additive XP bonus awarded on this day.</param>
/// <param name="CapNextDay">Whether the next day should be capped/plateaued.</param>
public sealed record StreakCurveEntry(
    int DayIndex,
    decimal Multiplier,
    int AdditiveBonusXp,
    bool CapNextDay);
