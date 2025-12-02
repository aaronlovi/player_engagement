using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

namespace PlayerEngagement.Host.Contracts.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Payload used to create a new draft policy version for a given policy key.
/// </summary>
public sealed record CreatePolicyVersionRequest {
    /// <summary>Operator-facing display name for the policy.</summary>
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>Optional description of the policy intent.</summary>
    public string? Description { get; init; }
    /// <summary>Base XP amount awarded before multipliers.</summary>
    public int BaseXpAmount { get; init; }
    /// <summary>Currency code for the XP grant.</summary>
    public string Currency { get; init; } = "XP";
    /// <summary>Minutes offset from midnight when the claim window starts.</summary>
    public int ClaimWindowStartMinutes { get; init; }
    /// <summary>Duration of the claim window in hours.</summary>
    public int ClaimWindowDurationHours { get; init; }
    /// <summary>Anchor strategy used to calculate reward days.</summary>
    public string AnchorStrategy { get; init; } = string.Empty;
    /// <summary>Number of allowed missed days in the grace window.</summary>
    public int GraceAllowedMisses { get; init; }
    /// <summary>Days in the grace window.</summary>
    public int GraceWindowDays { get; init; }
    /// <summary>Streak model type identifier.</summary>
    public string StreakModelType { get; init; } = string.Empty;
    /// <summary>Model-specific parameters as a key/value bag.</summary>
    public Dictionary<string, object?> StreakModelParameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Sample window used for preview calculations.</summary>
    public int PreviewSampleWindowDays { get; init; } = 7;
    /// <summary>Optional default segment used when previewing.</summary>
    public string? PreviewDefaultSegment { get; init; }
    /// <summary>Ordered streak curve entries.</summary>
    public List<StreakCurveEntry> StreakCurve { get; init; } = [];
    /// <summary>Seasonal boost windows to apply.</summary>
    public List<SeasonalBoost> SeasonalBoosts { get; init; } = [];
    /// <summary>Optional future effective time for the draft.</summary>
    public DateTime? EffectiveAt { get; init; }
}
