using System;

namespace PlayerEngagement.Domain.Policies;

/// <summary>Represents a seasonal boost window for a policy version.</summary>
/// <param name="BoostId">Unique identifier for the boost entry.</param>
/// <param name="Label">Operator-facing label describing the boost.</param>
/// <param name="Multiplier">Multiplier applied during the boost window.</param>
/// <param name="StartUtc">UTC timestamp when the boost becomes active.</param>
/// <param name="EndUtc">UTC timestamp when the boost ends.</param>
public sealed record SeasonalBoost(
    long BoostId,
    string Label,
    decimal Multiplier,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc);
