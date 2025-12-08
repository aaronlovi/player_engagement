using System;

namespace PlayerEngagement.Infrastructure.Persistence.DTOs.Seasons;

/// <summary>
/// DTO representing the current or upcoming season window.
/// </summary>
public sealed record SeasonCalendarDTO(
    long SeasonId,
    string Label,
    DateTime StartDate,
    DateTime EndDate)
{
    /// <summary>Empty instance for not-found cases.</summary>
    public static readonly SeasonCalendarDTO Empty = new(0, string.Empty, DateTime.MinValue, DateTime.MinValue);

    /// <summary>True when no season is present.</summary>
    public bool IsEmpty => SeasonId == 0;
}
