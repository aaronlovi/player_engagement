using System;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Persistence;
using PlayerEngagement.Infrastructure.Persistence.DTOs.Seasons;

namespace PlayerEngagement.Infrastructure.Seasons;

/// <summary>
/// Retrieves season boundaries from the database via the Dbm service. Caches the current season and blocks until initial load completes.
/// </summary>
public sealed class SeasonBoundaryProvider : ISeasonBoundaryProvider
{
    private readonly IPlayerEngagementDbmService _dbmService;
    private readonly ILogger<SeasonBoundaryProvider> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private volatile bool _loaded;
    private SeasonBoundaryInfo? _currentSeason;
    private SeasonBoundaryInfo? _nextSeason;

    public SeasonBoundaryProvider(IPlayerEngagementDbmService dbmService, ILoggerFactory loggerFactory)
    {
        _dbmService = dbmService ?? throw new ArgumentNullException(nameof(dbmService));
        _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger<SeasonBoundaryProvider>();
    }

    /// <inheritdoc />
    public async Task<SeasonBoundaryInfo?> GetCurrentSeasonAsync(CancellationToken cancellationToken)
    {
        if (!_loaded)
            await EnsureLoadedAsync(cancellationToken);

        return _currentSeason;
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken) => await EnsureLoadedAsync(cancellationToken, force: true);

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken, bool force = false)
    {
        if (_loaded && !force)
            return;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_loaded && !force)
                return;

            Result<SeasonCalendarWithNextDTO> result = await _dbmService.GetCurrentSeasonAsync(cancellationToken);
            if (result.IsFailure)
            {
                _logger.LogError("GetCurrentSeasonAsync failed: {Error}", result.ErrorMessage);
                return;
            }

            if (result.Value!.Current.IsEmpty)
            {
                _currentSeason = null;
                _nextSeason = MapToBoundary(result.Value.Next);
                _loaded = true;
                return;
            }

            _currentSeason = MapToBoundary(result.Value.Current);
            _nextSeason = MapToBoundary(result.Value.Next);
            _loaded = true;
        }
        finally
        {
            _ = _loadLock.Release();
        }
    }

    private static SeasonBoundaryInfo? MapToBoundary(SeasonCalendarDTO dto)
    {
        if (dto.IsEmpty)
            return null;

        var start = DateOnly.FromDateTime(dto.StartDate);
        var end = DateOnly.FromDateTime(dto.EndDate);
        return new SeasonBoundaryInfo(start, end);
    }
}
