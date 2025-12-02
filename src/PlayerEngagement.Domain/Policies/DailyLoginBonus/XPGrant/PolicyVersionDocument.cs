using System;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>Immutable representation of a single policy version.</summary>
/// <param name="PolicyVersion">Monotonic version number inside the policy.</param>
/// <param name="Status">Lifecycle status.</param>
/// <param name="BaseXpAmount">Base XP granted before streak adjustments.</param>
/// <param name="Currency">Currency name/code.</param>
/// <param name="ClaimWindowStartOffset">Daily start offset for the claim window.</param>
/// <param name="ClaimWindowDuration">Duration of the claim window.</param>
/// <param name="AnchorStrategy">Strategy used to translate UTC to reward days.</param>
/// <param name="GracePolicy">Grace policy definition.</param>
/// <param name="StreakModel">Streak model configuration.</param>
/// <param name="Preview">Operator preview settings.</param>
/// <param name="EffectiveAt">When the version becomes active.</param>
/// <param name="SupersededAt">When the version was superseded.</param>
/// <param name="CreatedAt">When the version was created.</param>
/// <param name="CreatedBy">Identity that created the version.</param>
/// <param name="PublishedAt">When the version was published (if applicable).</param>
public sealed record PolicyVersionDocument(
    long PolicyVersion,
    PolicyVersionStatus Status,
    int BaseXpAmount,
    string Currency,
    TimeSpan ClaimWindowStartOffset,
    TimeSpan ClaimWindowDuration,
    AnchorStrategy AnchorStrategy,
    GracePolicyDefinition GracePolicy,
    StreakModelDefinition StreakModel,
    PreviewSettings Preview,
    DateTime? EffectiveAt,
    DateTime? SupersededAt,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? PublishedAt);
