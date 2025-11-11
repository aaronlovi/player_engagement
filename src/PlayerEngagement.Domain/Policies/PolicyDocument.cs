using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Represents a fully materialized policy version plus the dependent collections (streak curve, seasonal boosts).
/// </summary>
/// <param name="PolicyKey">Logical policy identifier (e.g., daily-login-xp).</param>
/// <param name="DisplayName">Operator-facing display name.</param>
/// <param name="Description">Optional description for operator context.</param>
/// <param name="Version">Immutable version metadata and configuration values.</param>
/// <param name="StreakCurve">Ordered streak curve entries associated with the version.</param>
/// <param name="SeasonalBoosts">Seasonal boost windows tied to the version.</param>
public sealed record PolicyDocument(
    string PolicyKey,
    string DisplayName,
    string Description,
    PolicyVersionDocument Version,
    IReadOnlyList<StreakCurveEntry> StreakCurve,
    IReadOnlyList<SeasonalBoost> SeasonalBoosts);
