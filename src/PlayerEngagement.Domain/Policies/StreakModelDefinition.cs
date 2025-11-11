using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Defines the streak model type and any arbitrary parameters it requires.
/// </summary>
/// <param name="Type">The model type to apply.</param>
/// <param name="Parameters">Loose dictionary of parameters consumed by the model.</param>
public sealed record StreakModelDefinition(
    StreakModelType Type,
    IReadOnlyDictionary<string, object?> Parameters);
