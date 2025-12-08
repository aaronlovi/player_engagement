using System;

namespace PlayerEngagement.Infrastructure.Persistence.DTOs.Seasons;

/// <summary>
/// Represents the current season and optional next season for scheduling refresh.
/// </summary>
/// <param name="Current">Current season calendar entry.</param>
/// <param name="Next">Next season calendar entry (empty when none).</param>
public sealed record SeasonCalendarWithNextDTO(
    SeasonCalendarDTO Current,
    SeasonCalendarDTO Next)
{
    public static readonly SeasonCalendarWithNextDTO Empty = new(SeasonCalendarDTO.Empty, SeasonCalendarDTO.Empty);
}
