using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

namespace PlayerEngagement.Domain.Tests.Policies.DailyLoginBonus.XPGrant;

internal static class PolicyDocumentFactory
{
    public static PolicyVersionDocument CreatePolicyVersionDocument(
        long? policyVersion = null,
        PolicyVersionStatus status = PolicyVersionStatus.Published,
        int? baseXpAmount = null,
        string? currency = null,
        TimeSpan? claimWindowStartOffset = null,
        TimeSpan? claimWindowDuration = null,
        AnchorStrategy anchorStrategy = AnchorStrategy.AnchorTimezone,
        GracePolicyDefinition? gracePolicy = null,
        StreakModelDefinition? streakModel = null,
        PreviewSettings? preview = null,
        DateTime? effectiveAt = null,
        DateTime? supersededAt = null,
        DateTime? createdAt = null,
        string? createdBy = null,
        DateTime? publishedAt = null)
    {
        return new PolicyVersionDocument(
            policyVersion ?? 1,
            status,
            baseXpAmount ?? 100,
            currency ?? "XP",
            claimWindowStartOffset ?? TimeSpan.Zero,
            claimWindowDuration ?? TimeSpan.FromHours(24),
            anchorStrategy,
            gracePolicy ?? new GracePolicyDefinition(0, 0),
            streakModel ?? new PlateauCapStreakModel(1, 1m),
            preview ?? new PreviewSettings(7, null),
            effectiveAt,
            supersededAt,
            createdAt ?? DateTime.UtcNow,
            createdBy ?? "test",
            publishedAt);
    }

    public static PolicyDocument CreatePolicyDocument(
        string? policyKey = null,
        string? displayName = null,
        string? description = null,
        PolicyVersionDocument? version = null,
        IReadOnlyList<StreakCurveEntry>? streakCurve = null,
        IReadOnlyList<SeasonalBoost>? seasonalBoosts = null)
    {
        return new PolicyDocument(
            policyKey ?? "daily-login-xp",
            displayName ?? "Daily Login XP",
            description ?? "Test policy",
            version ?? CreatePolicyVersionDocument(),
            streakCurve ?? DefaultStreakCurve(),
            seasonalBoosts ?? Array.Empty<SeasonalBoost>());
    }

    public static PolicyDocument CreatePlateauPolicy(
        int plateauDay = 3,
        decimal plateauMultiplier = 2m,
        int? baseXp = null,
        GracePolicyDefinition? grace = null,
        IReadOnlyList<StreakCurveEntry>? streakCurve = null)
    {
        PlateauCapStreakModel model = new(plateauDay, plateauMultiplier);
        PolicyVersionDocument version = CreatePolicyVersionDocument(
            baseXpAmount: baseXp,
            streakModel: model,
            gracePolicy: grace ?? new GracePolicyDefinition(1, 1));

        return CreatePolicyDocument(
            description: "Plateau test policy",
            version: version,
            streakCurve: streakCurve ?? DefaultPlateauCurve());
    }

    public static PolicyDocument CreateDecayPolicy(
        decimal decayPercent,
        int graceAllowedMisses,
        int graceWindowDays,
        int? baseXp = null,
        IReadOnlyList<StreakCurveEntry>? streakCurve = null)
    {
        DecayCurveStreakModel model = new(decayPercent, graceWindowDays);
        GracePolicyDefinition grace = new(graceAllowedMisses, graceWindowDays);
        PolicyVersionDocument version = CreatePolicyVersionDocument(
            baseXpAmount: baseXp,
            streakModel: model,
            gracePolicy: grace);

        return CreatePolicyDocument(
            description: "Decay test policy",
            version: version,
            streakCurve: streakCurve ?? DefaultDecayCurve());
    }

    private static IReadOnlyList<StreakCurveEntry> DefaultPlateauCurve()
    {
        return
        [
            new StreakCurveEntry(0, 1m, 0, false),
            new StreakCurveEntry(1, 1.1m, 0, false),
            new StreakCurveEntry(2, 1.2m, 0, false)
        ];
    }

    private static IReadOnlyList<StreakCurveEntry> DefaultDecayCurve()
    {
        return
        [
            new StreakCurveEntry(0, 1m, 0, false),
            new StreakCurveEntry(1, 1.1m, 0, false),
            new StreakCurveEntry(2, 1.2m, 0, false),
            new StreakCurveEntry(3, 1.3m, 0, false),
            new StreakCurveEntry(4, 1.4m, 0, false),
            new StreakCurveEntry(5, 1.5m, 0, false)
        ];
    }

    private static IReadOnlyList<StreakCurveEntry> DefaultStreakCurve()
    {
        return
        [
            new StreakCurveEntry(0, 1m, 0, false)
        ];
    }
}
