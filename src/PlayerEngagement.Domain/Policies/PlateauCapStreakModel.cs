using System;

namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Plateau/Cap streak model where growth stops after the plateau day.
/// </summary>
public sealed record PlateauCapStreakModel : StreakModelDefinition {
    public PlateauCapStreakModel(int plateauDay, decimal plateauMultiplier)
        : base(StreakModelType.PlateauCap) {
        if (plateauDay < 1)
            throw new ArgumentOutOfRangeException(nameof(plateauDay), plateauDay, "PlateauDay must be at least 1.");
        if (plateauMultiplier <= 0m)
            throw new ArgumentOutOfRangeException(nameof(plateauMultiplier), plateauMultiplier, "PlateauMultiplier must be greater than 0.");

        PlateauDay = plateauDay;
        PlateauMultiplier = plateauMultiplier;
    }

    /// <summary>Day index (1-based) when the plateau is reached.</summary>
    public int PlateauDay { get; }

    /// <summary>Multiplier applied at and after the plateau.</summary>
    public decimal PlateauMultiplier { get; }
}
