using System;

namespace PlayerEngagement.Shared.Validation;

/// <summary>
/// Represents an inclusive integer range.
/// </summary>
public readonly record struct IntRange {
    public IntRange(int start, int end) {
        if (end < start)
            throw new ArgumentOutOfRangeException(nameof(end), end, "End must be greater than or equal to Start.");

        Start = start;
        End = end;
    }

    public int Start { get; }

    public int End { get; }
}
