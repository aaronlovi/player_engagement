using System;
using System.Collections.Generic;

namespace PlayerEngagement.Domain.Policies;

/// <summary>
/// Temporary holder for streak models that are not yet represented by a typed definition.
/// </summary>
/// <param name="Type">Model type.</param>
/// <param name="Parameters">Raw parameter bag.</param>
public sealed record RawStreakModelDefinition(
    StreakModelType Type,
    IReadOnlyDictionary<string, object?> Parameters) : StreakModelDefinition(
        Type != StreakModelType.Invalid ? Type : throw new ArgumentOutOfRangeException(nameof(Type), "StreakModelType cannot be Invalid.")) {
    public IReadOnlyDictionary<string, object?> Parameters { get; } = Parameters ?? throw new ArgumentNullException(nameof(Parameters));

    public RawStreakModelDefinition(StreakModelType type)
        : this(type, new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)) {
        if (type == StreakModelType.Invalid)
            throw new ArgumentOutOfRangeException(nameof(type), "StreakModelType cannot be Invalid.");
    }
}
