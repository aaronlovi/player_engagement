using System;
using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Tiered seasonal reset model with non-overlapping tiers.
/// </summary>
public sealed record TieredSeasonalResetStreakModel : StreakModelDefinition {
    public TieredSeasonalResetStreakModel(IReadOnlyList<TieredSeasonalResetTierDefinition> tiers)
        : base(StreakModelType.TieredSeasonalReset) {
        ArgumentNullException.ThrowIfNull(tiers);
        if (tiers.Count == 0)
            throw new ArgumentException("At least one tier is required.", nameof(tiers));

        ValidateNonOverlapping(tiers);
        Tiers = tiers;
    }

    public IReadOnlyList<TieredSeasonalResetTierDefinition> Tiers { get; }

    private static void ValidateNonOverlapping(IReadOnlyList<TieredSeasonalResetTierDefinition> tiers) {
        List<TieredSeasonalResetTierDefinition> ordered = new(tiers);
        ordered.Sort(static (a, b) => {
            int startCompare = a.StartDay.CompareTo(b.StartDay);
            return startCompare != 0 ? startCompare : a.EndDay.CompareTo(b.EndDay);
        });

        TieredSeasonalResetTierDefinition? previous = null;
        for (int i = 0; i < ordered.Count; i++) {
            TieredSeasonalResetTierDefinition current = ordered[i];
            if (previous is not null && current.StartDay <= previous.EndDay)
                throw new ArgumentOutOfRangeException(nameof(tiers), $"Tiers overlap: [{previous.StartDay},{previous.EndDay}] and [{current.StartDay},{current.EndDay}].");

            previous = current;
        }
    }
}
