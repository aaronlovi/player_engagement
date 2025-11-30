using System.Collections.Generic;

namespace PlayerEngagement.Host.Contracts.Policies;

/// <summary>
/// Payload for replacing the set of segment overrides under a policy key.
/// </summary>
public sealed record UpdateSegmentOverridesRequest {
    /// <summary>Segment to policy-version map that replaces existing overrides.</summary>
    public Dictionary<string, long> Overrides { get; init; } = new();
}
