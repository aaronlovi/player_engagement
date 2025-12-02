using System;

namespace PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;

/// <summary>
/// Input DTO used when creating a new policy draft version.
/// </summary>
public sealed record PolicyVersionWriteDto(
    string PolicyKey,
    string DisplayName,
    string? Description,
    int BaseXpAmount,
    string Currency,
    int ClaimWindowStartMinutes,
    int ClaimWindowDurationHours,
    string AnchorStrategy,
    int GraceAllowedMisses,
    int GraceWindowDays,
    string StreakModelType,
    string StreakModelParameters,
    int PreviewSampleWindowDays,
    string? PreviewDefaultSegment,
    string SeasonalMetadata,
    DateTime? EffectiveAt,
    DateTime CreatedAt,
    string CreatedBy,
    long PolicyVersion,
    long? PolicyId);
