namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Evaluates streak transitions for daily login XP grant policies.
/// Kept as a single engine surface for DI/testing; model differences live inside the implementation rather than separate engines.
/// </summary>
public interface IStreakEngine
{
    /// <summary>Computes the next streak state and XP outcome for a claim.</summary>
    /// <param name="context">Transition context containing policy, prior state, and reward-day info.</param>
    /// <returns>Deterministic transition result.</returns>
    StreakTransitionResult Evaluate(StreakTransitionContext context);
}
