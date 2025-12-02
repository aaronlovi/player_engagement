using System;
using System.Collections.Generic;

namespace PlayerEngagement.Host.Contracts.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Payload submitted when publishing an existing draft or archived policy version.
/// </summary>
public sealed record PublishPolicyVersionRequest {
    /// <summary>Optional UTC timestamp when the version should become effective.</summary>
    public DateTime? EffectiveAt { get; init; }
    /// <summary>Optional map of segment keys to target policy versions.</summary>
    public Dictionary<string, long>? SegmentOverrides { get; init; }
}
