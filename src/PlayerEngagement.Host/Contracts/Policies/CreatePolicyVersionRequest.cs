using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies;

namespace PlayerEngagement.Host.Contracts.Policies;

/// <summary>
/// Payload used to create a new draft policy version for a given policy key.
/// </summary>
public sealed record CreatePolicyVersionRequest {
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int BaseXpAmount { get; init; }
    public string Currency { get; init; } = "XP";
    public int ClaimWindowStartMinutes { get; init; }
    public int ClaimWindowDurationHours { get; init; }
    public string AnchorStrategy { get; init; } = string.Empty;
    public int GraceAllowedMisses { get; init; }
    public int GraceWindowDays { get; init; }
    public string StreakModelType { get; init; } = string.Empty;
    public Dictionary<string, object?> StreakModelParameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public int PreviewSampleWindowDays { get; init; } = 7;
    public string? PreviewDefaultSegment { get; init; }
    public List<StreakCurveEntry> StreakCurve { get; init; } = [];
    public List<SeasonalBoost> SeasonalBoosts { get; init; } = [];
    public DateTime? EffectiveAt { get; init; }
}

