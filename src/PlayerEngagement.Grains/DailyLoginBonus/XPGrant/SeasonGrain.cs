using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using PlayerEngagement.Grains.Interfaces.DailyLoginBonus.XPGrant;

namespace PlayerEngagement.Grains.DailyLoginBonus.XPGrant;

/// <summary>
/// Single-instance grain that loads season boundaries from persistence and caches them for callers.
/// </summary>
public sealed class SeasonGrain : Grain, ISeasonGrain
{
    private readonly ISeasonBoundaryProvider _seasonBoundaryProvider;
    private readonly ILogger<SeasonGrain> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private volatile bool _loaded;
    private SeasonBoundaryInfo? _currentSeason;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonGrain"/> class.
    /// </summary>
    /// <param name="seasonBoundaryProvider">Provider used to fetch season boundaries from the database.</param>
    /// <param name="loggerFactory">Factory used to create the grain logger.</param>
    public SeasonGrain(ISeasonBoundaryProvider seasonBoundaryProvider, ILoggerFactory loggerFactory)
    {
        _seasonBoundaryProvider = seasonBoundaryProvider ?? throw new ArgumentNullException(nameof(seasonBoundaryProvider));
        _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory))).CreateLogger<SeasonGrain>();
    }

    /// <inheritdoc />
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        long grainId = this.GetPrimaryKeyLong();
        if (grainId != 0)
            throw new InvalidOperationException($"SeasonGrain must be activated with key 0; received {grainId}.");

        _logger.LogInformation("SeasonGrain activation started.");
        await EnsureLoadedAsync(cancellationToken, forceRefresh: true);
        await base.OnActivateAsync(cancellationToken);
        _logger.LogInformation("SeasonGrain activation completed.");
    }

    /// <inheritdoc />
    public async Task<SeasonBoundaryInfo?> GetCurrentSeasonAsync()
    {
        _logger.LogInformation("GetCurrentSeasonAsync invoked.");
        await EnsureLoadedAsync(CancellationToken.None, forceRefresh: false);
        return _currentSeason;
    }

    /// <inheritdoc />
    public async Task RefreshAsync()
    {
        _logger.LogInformation("RefreshAsync invoked.");
        await EnsureLoadedAsync(CancellationToken.None, forceRefresh: true);
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken, bool forceRefresh)
    {
        if (_loaded && !forceRefresh)
            return;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_loaded && !forceRefresh)
                return;

            _logger.LogInformation("Loading season boundaries in SeasonGrain (force={Force})", forceRefresh);
            if (forceRefresh)
                await _seasonBoundaryProvider.RefreshAsync(cancellationToken);

            _currentSeason = await _seasonBoundaryProvider.GetCurrentSeasonAsync(cancellationToken);
            _loaded = true;

            bool hasSeason = _currentSeason is not null;
            _logger.LogInformation("SeasonGrain load complete: hasCurrentSeason={HasSeason}", hasSeason);
        }
        catch (Exception ex)
        {
            _loaded = false;
            _logger.LogError(ex, "SeasonGrain load failed.");
            throw;
        }
        finally
        {
            _ = _loadLock.Release();
        }
    }
}
