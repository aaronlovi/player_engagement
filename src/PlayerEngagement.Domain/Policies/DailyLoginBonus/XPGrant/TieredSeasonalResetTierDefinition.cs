using System;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Defines a tier for the tiered seasonal reset streak model.
/// </summary>
public sealed record TieredSeasonalResetTierDefinition {
    public TieredSeasonalResetTierDefinition(int startDay, int endDay, decimal bonusMultiplier) {
        if (startDay < 1)
            throw new ArgumentOutOfRangeException(nameof(startDay), startDay, "StartDay must be at least 1.");
        if (endDay < startDay)
            throw new ArgumentOutOfRangeException(nameof(endDay), endDay, "EndDay must be >= StartDay.");
        if (bonusMultiplier <= 0m)
            throw new ArgumentOutOfRangeException(nameof(bonusMultiplier), bonusMultiplier, "BonusMultiplier must be greater than 0.");

        StartDay = startDay;
        EndDay = endDay;
        BonusMultiplier = bonusMultiplier;
    }

    public int StartDay { get; }

    public int EndDay { get; }

    public decimal BonusMultiplier { get; }
}
