using System;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Decay curve streak model that applies a decay percent after grace.
/// </summary>
public sealed record DecayCurveStreakModel : StreakModelDefinition {
    public DecayCurveStreakModel(decimal decayPercent, int graceDay)
        : base(StreakModelType.DecayCurve) {
        if (decayPercent is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(decayPercent), decayPercent, "DecayPercent must be between 0 and 1 inclusive.");
        if (graceDay < 0)
            throw new ArgumentOutOfRangeException(nameof(graceDay), graceDay, "GraceDay must be non-negative.");

        DecayPercent = decayPercent;
        GraceDay = graceDay;
    }

    public decimal DecayPercent { get; }

    public int GraceDay { get; }
}
