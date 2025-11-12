using System;
using System.Collections.Generic;

namespace PlayerEngagement.Host.Contracts.Policies;

/// <summary>
/// Payload submitted when publishing an existing draft or archived policy version.
/// </summary>
public sealed record PublishPolicyVersionRequest {
    public DateTime? EffectiveAt { get; init; }
    public Dictionary<string, long>? SegmentOverrides { get; init; }
}
