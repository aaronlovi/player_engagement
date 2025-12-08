using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Persistence.DTOs.Seasons;
using PlayerEngagement.Infrastructure.Persistence;
using PlayerEngagement.Infrastructure.Seasons;
using Xunit;

namespace PlayerEngagement.Infrastructure.Tests.Seasons;

public sealed class SeasonBoundaryProviderTests
{
    [Fact]
    public async Task GetCurrentSeason_LoadsOnceAndReturnsBoundary()
    {
        PlayerEngagementDbmInMemoryData.SetCurrentSeason(new SeasonCalendarDTO(
            1,
            "Test",
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31)));

        PlayerEngagementDbmInMemoryService dbm = new(new NullLoggerFactory());
        SeasonBoundaryProvider provider = new(dbm, NullLoggerFactory.Instance);

        SeasonBoundaryInfo? result = await provider.GetCurrentSeasonAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2024, 1, 1), result!.SeasonStart);
        Assert.Equal(new DateOnly(2024, 1, 31), result.SeasonEnd);
    }

    [Fact]
    public async Task GetCurrentSeason_ReturnsNullWhenEmpty()
    {
        PlayerEngagementDbmInMemoryData.SetCurrentSeason(SeasonCalendarDTO.Empty);
        PlayerEngagementDbmInMemoryService dbm = new(new NullLoggerFactory());
        SeasonBoundaryProvider provider = new(dbm, NullLoggerFactory.Instance);

        SeasonBoundaryInfo? result = await provider.GetCurrentSeasonAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Refresh_ForcesReload()
    {
        PlayerEngagementDbmInMemoryData.SetCurrentSeason(new SeasonCalendarDTO(
            1,
            "One",
            new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31)));

        PlayerEngagementDbmInMemoryService dbm = new(new NullLoggerFactory());
        SeasonBoundaryProvider provider = new(dbm, NullLoggerFactory.Instance);
        _ = await provider.GetCurrentSeasonAsync(CancellationToken.None);

        PlayerEngagementDbmInMemoryData.SetCurrentSeason(new SeasonCalendarDTO(
            2,
            "Two",
            new DateTime(2024, 2, 1),
            new DateTime(2024, 2, 28)));

        await provider.RefreshAsync(CancellationToken.None);
        SeasonBoundaryInfo? result = await provider.GetCurrentSeasonAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2024, 2, 1), result!.SeasonStart);
    }
}
