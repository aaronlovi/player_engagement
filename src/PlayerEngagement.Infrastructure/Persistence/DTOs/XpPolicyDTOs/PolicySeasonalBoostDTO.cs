using System;

namespace PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

/// <summary>
/// DTO representing a seasonal boost window for a policy version.
/// </summary>
public sealed record PolicySeasonalBoostDTO(
    long BoostId,
    string PolicyKey,
    int PolicyVersion,
    string Label,
    decimal Multiplier,
    DateTime StartUtc,
    DateTime EndUtc) {
    public static readonly PolicySeasonalBoostDTO Empty = new(
        0, string.Empty, 0, string.Empty, 0m, DateTime.MinValue, DateTime.MinValue);

    public bool IsEmpty => BoostId == 0;
}
