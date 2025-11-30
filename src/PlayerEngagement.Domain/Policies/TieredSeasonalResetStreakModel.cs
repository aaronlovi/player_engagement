using System;
using System.Collections.Generic;
using PlayerEngagement.Shared.Validation;

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
        List<IntRange> ranges = new(tiers.Count);
        foreach (TieredSeasonalResetTierDefinition tier in tiers)
            ranges.Add(new IntRange(tier.StartDay, tier.EndDay));

        RangeValidation.EnsureNonOverlapping(ranges);
    }
}
