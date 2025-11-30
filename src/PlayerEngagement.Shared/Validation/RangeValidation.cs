using System;
using System.Collections.Generic;

namespace PlayerEngagement.Shared.Validation;

/// <summary>
/// Validation helpers for ranges.
/// </summary>
public static class RangeValidation {
    /// <summary>
    /// Validates that ranges are non-overlapping when ordered by start then end.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if an overlap is found.
    /// </summary>
    /// <param name="ranges">Ranges to validate.</param>
    public static void EnsureNonOverlapping(IReadOnlyList<IntRange> ranges) {
        ArgumentNullException.ThrowIfNull(ranges);

        if (ranges.Count <= 1)
            return;

        List<IntRange> ordered = [.. ranges];
        ordered.Sort(static (a, b) => {
            int startCompare = a.Start.CompareTo(b.Start);
            return startCompare != 0 ? startCompare : a.End.CompareTo(b.End);
        });

        IntRange previous = ordered[0];
        for (int i = 1; i < ordered.Count; i++) {
            IntRange current = ordered[i];
            if (current.Start <= previous.End)
                throw new ArgumentOutOfRangeException(nameof(ranges), $"Ranges overlap: [{previous.Start},{previous.End}] and [{current.Start},{current.End}].");

            previous = current;
        }
    }
}
