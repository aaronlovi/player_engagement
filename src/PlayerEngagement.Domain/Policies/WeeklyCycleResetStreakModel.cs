using System;

namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Weekly cycle reset streak model with a fixed cycle length.
/// </summary>
public sealed record WeeklyCycleResetStreakModel : StreakModelDefinition {
    public WeeklyCycleResetStreakModel()
        : base(StreakModelType.WeeklyCycleReset) { }

    /// <summary>Length of the cycle in days (fixed at 7).</summary>
    public const int CycleLength = 7;
}
