using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;

/// <summary>
/// Base streak model definition. Specific models derive from this type.
/// </summary>
/// <param name="Type">The model type to apply.</param>
public abstract record StreakModelDefinition(
    StreakModelType Type);
