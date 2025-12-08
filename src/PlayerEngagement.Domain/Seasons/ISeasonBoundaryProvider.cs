using System.Threading;
using System.Threading.Tasks;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

namespace PlayerEngagement.Domain.Seasons;

/// <summary>
/// Provides season boundary information for streak computations.
/// </summary>
public interface ISeasonBoundaryProvider
{
    /// <summary>Returns the current season boundaries if active; null when no season is active.</summary>
    Task<SeasonBoundaryInfo?> GetCurrentSeasonAsync(CancellationToken cancellationToken);

    /// <summary>Forces a reload from the backing store.</summary>
    Task RefreshAsync(CancellationToken cancellationToken);
}
