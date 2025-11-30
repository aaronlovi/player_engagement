using System;

namespace PlayerEngagement.Host.Contracts.Policies;

/// <summary>
/// Payload sent when retiring a published policy version.
/// </summary>
public sealed record RetirePolicyVersionRequest {
    /// <summary>Optional UTC timestamp for the retirement to take effect.</summary>
    public DateTime? RetiredAt { get; init; }
}
