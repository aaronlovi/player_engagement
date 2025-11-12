using System.Collections.Generic;

namespace PlayerEngagement.Host.Contracts.Policies;

/// <summary>
/// Payload for replacing the set of segment overrides under a policy key.
/// </summary>
public sealed record UpdateSegmentOverridesRequest {
    public Dictionary<string, int> Overrides { get; init; } = new();
}
