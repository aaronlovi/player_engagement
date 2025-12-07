using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Default implementation of the streak engine that evaluates model transitions.
/// A single engine serves all models; plug-ins inside the class handle model-specific rules rather than using separate engine types.
/// </summary>
public sealed class StreakEngine : IStreakEngine
{
    private const int MinimumStreakDay = 1;
    private readonly ILogger<StreakEngine> _logger;

    /// <summary>Initializes a new instance using a null logger (primarily for tests).</summary>
    public StreakEngine()
        : this(NullLoggerFactory.Instance)
    {
    }

    /// <summary>Initializes a new instance with a logger factory.</summary>
    /// <param name="loggerFactory">Factory used to create a typed logger.</param>
    public StreakEngine(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _logger = loggerFactory.CreateLogger<StreakEngine>();
    }

    /// <inheritdoc />
    public StreakTransitionResult Evaluate(StreakTransitionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        PolicyDocument policy = context.Policy;
        StreakModelDefinition model = policy.Version.StreakModel;
        GracePolicyDefinition gracePolicy = policy.Version.GracePolicy;

        int graceUsed = context.PriorState.GraceUsed;
        int baseStreak = CalculateBaseStreak(context.PriorState, context.RewardDayId, gracePolicy, out bool graceApplied, ref graceUsed, model);

        StreakEngineModelComputation modelComputation = ApplyModelRules(model, baseStreak, context.PriorState.ModelState, context.RewardDayId);

        StreakCurveEntry curveEntry = ResolveCurveEntry(policy.StreakCurve, modelComputation.EffectiveStreakDay);

        decimal effectiveMultiplier = curveEntry.Multiplier * modelComputation.ModelMultiplier;
        int additiveBonusXp = curveEntry.AdditiveBonusXp + modelComputation.AdditiveBonusXp;

        int xpAwarded = CalculateAwardedXp(policy.Version.BaseXpAmount, effectiveMultiplier, additiveBonusXp);

        int longest = context.PriorState.LongestStreak;
        if (modelComputation.EffectiveStreakDay > longest)
            longest = modelComputation.EffectiveStreakDay;

        StreakState newState = new(
            modelComputation.EffectiveStreakDay,
            longest,
            graceUsed,
            context.RewardDayId,
            modelComputation.ModelState);

        return new StreakTransitionResult(
            newState,
            modelComputation.EffectiveStreakDay,
            effectiveMultiplier,
            additiveBonusXp,
            xpAwarded,
            graceApplied,
            modelComputation.MilestoneHits);
    }

    private static int CalculateBaseStreak(
        StreakState priorState,
        DateOnly rewardDayId,
        GracePolicyDefinition gracePolicy,
        out bool graceApplied,
        ref int graceUsed,
        StreakModelDefinition model)
    {
        int currentStreak = priorState.CurrentStreak;
        DateOnly? lastRewardDayId = priorState.LastRewardDayId;
        graceApplied = false;

        if (!lastRewardDayId.HasValue)
            return MinimumStreakDay;

        int gapDays = rewardDayId.DayNumber - lastRewardDayId.Value.DayNumber;
        if (gapDays <= 0)
            return currentStreak;

        if (gapDays == 1)
            return currentStreak + 1;

        int missedDays = gapDays - 1;
        if (gracePolicy.AllowedMisses > 0 &&
            graceUsed + missedDays <= gracePolicy.AllowedMisses &&
            missedDays <= gracePolicy.WindowDays)
        {
            graceUsed += missedDays;
            graceApplied = true;
            return currentStreak + 1;
        }

        if (model is DecayCurveStreakModel decayModel)
        {
            decimal decayed = currentStreak * (1m - decayModel.DecayPercent);
            int floored = (int)Math.Floor(decayed);
            floored = Math.Max(floored, MinimumStreakDay);
            return floored;
        }

        return MinimumStreakDay;
    }

    private StreakEngineModelComputation ApplyModelRules(
        StreakModelDefinition model,
        int baseStreak,
        StreakModelRuntimeState priorModelState,
        DateOnly rewardDayId)
    {
        StreakEngineModelComputation computation = new()
        {
            EffectiveStreakDay = baseStreak,
            ModelMultiplier = 1m,
            AdditiveBonusXp = 0,
            ModelState = priorModelState ?? StreakModelRuntimeState.Empty,
            MilestoneHits = []
        };

        switch (model.Type)
        {
            case StreakModelType.PlateauCap:
                ApplyPlateau(model as PlateauCapStreakModel, ref computation);
                break;
            case StreakModelType.WeeklyCycleReset:
                ApplyWeeklyCycle(ref computation);
                break;
            case StreakModelType.DecayCurve:
                ApplyDecay(ref computation);
                break;
            case StreakModelType.TieredSeasonalReset:
                ApplyTieredSeasonal(model as TieredSeasonalResetStreakModel, rewardDayId, ref computation);
                break;
            case StreakModelType.MilestoneMetaReward:
                ApplyMilestone(model as MilestoneMetaRewardStreakModel, ref computation);
                break;
            default:
                break;
        }

        computation.EffectiveStreakDay = Math.Max(computation.EffectiveStreakDay, MinimumStreakDay);
        return computation;
    }

    private void ApplyPlateau(PlateauCapStreakModel? model, ref StreakEngineModelComputation computation)
    {
        if (model is null)
        {
            _logger.LogError("Streak model type {ModelType} missing PlateauCap configuration.", StreakModelType.PlateauCap);
            return;
        }

        if (computation.EffectiveStreakDay >= model.PlateauDay)
        {
            computation.EffectiveStreakDay = model.PlateauDay;
            computation.ModelMultiplier = model.PlateauMultiplier;
        }
    }

    private void ApplyWeeklyCycle(ref StreakEngineModelComputation computation)
    {
        if (computation.EffectiveStreakDay > WeeklyCycleResetStreakModel.CycleLength)
            computation.EffectiveStreakDay = MinimumStreakDay;
    }

    private void ApplyDecay(ref StreakEngineModelComputation computation)
    {
        if (computation.EffectiveStreakDay < MinimumStreakDay)
            computation.EffectiveStreakDay = MinimumStreakDay;
    }

    private void ApplyTieredSeasonal(
        TieredSeasonalResetStreakModel? model,
        DateOnly rewardDayId,
        ref StreakEngineModelComputation computation)
    {
        // Seasonal reset boundary detection is not wired yet; placeholder keeps streak as-is.
        // Tier selection could adjust multiplier; currently no additional multiplier applied.
        if (model is null)
        {
            _logger.LogError("Streak model type {ModelType} missing TieredSeasonalReset configuration.", StreakModelType.TieredSeasonalReset);
            return;
        }

        _ = rewardDayId;
        _ = computation;
    }

    private void ApplyMilestone(
        MilestoneMetaRewardStreakModel? model,
        ref StreakEngineModelComputation computation)
    {
        if (model is null)
        {
            _logger.LogError("Streak model type {ModelType} missing MilestoneMetaReward configuration.", StreakModelType.MilestoneMetaReward);
            return;
        }

        IReadOnlyCollection<int> claimed = computation.ModelState.ClaimedMilestones;
        List<MilestoneMetaRewardMilestone> hits = new(model.Milestones.Count);
        List<int> updatedClaimed = new(model.Milestones.Count);

        foreach (int claimedDay in claimed)
            updatedClaimed.Add(claimedDay);

        foreach (MilestoneMetaRewardMilestone milestone in model.Milestones)
        {
            bool alreadyClaimed = false;
            for (int i = 0; i < updatedClaimed.Count && !alreadyClaimed; i++) {
                if (updatedClaimed[i] != milestone.Day)
                    continue;
                alreadyClaimed = true;
            }

            if (alreadyClaimed)
                continue;

            if (computation.EffectiveStreakDay == milestone.Day)
            {
                hits.Add(milestone);
                updatedClaimed.Add(milestone.Day);
            }
        }

        if (hits.Count > 0)
        {
            computation.MilestoneHits = [.. hits];
            computation.ModelState = new StreakModelRuntimeState([.. updatedClaimed]);
        }
    }

    private static StreakCurveEntry ResolveCurveEntry(IReadOnlyList<StreakCurveEntry> curve, int effectiveStreakDay)
    {
        if (curve == null || curve.Count == 0)
            throw new InvalidOperationException("Streak curve must contain at least one entry.");

        int targetIndex = effectiveStreakDay - 1;
        StreakCurveEntry selected = curve[0];
        for (int i = 0; i < curve.Count; i++)
        {
            StreakCurveEntry entry = curve[i];
            if (entry.DayIndex <= targetIndex && entry.DayIndex >= selected.DayIndex)
                selected = entry;
            if (entry.DayIndex == targetIndex)
                break;
        }

        return selected;
    }

    private static int CalculateAwardedXp(int baseXpAmount, decimal multiplier, int additiveBonusXp)
    {
        decimal multiplied = baseXpAmount * multiplier;
        int rounded = (int)Math.Round(multiplied, MidpointRounding.AwayFromZero);
        return rounded + additiveBonusXp;
    }
}
