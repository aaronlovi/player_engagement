using System.Threading.Tasks;
using Orleans;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

namespace PlayerEngagement.Grains.Interfaces.DailyLoginBonus.XPGrant;

/// <summary>
/// Orleans grain that surfaces season boundaries for streak calculations (singleton keyed as 0).
/// </summary>
public interface ISeasonGrain : IGrainWithIntegerKey
{
    /// <summary>
    /// Returns the current season boundaries; null when no season is active.
    /// </summary>
    Task<SeasonBoundaryInfo?> GetCurrentSeasonAsync();

    /// <summary>
    /// Forces a reload from persistence after admin changes or season rollover.
    /// </summary>
    Task RefreshAsync();
}
