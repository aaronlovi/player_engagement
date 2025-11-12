using System;

namespace PlayerEngagement.Host.Contracts.Policies;

/// <summary>
/// Payload sent when retiring a published policy version.
/// </summary>
public sealed record RetirePolicyVersionRequest {
    public DateTime? RetiredAt { get; init; }
}
