using System;
using System.Collections.Generic;
using System.Globalization;

namespace PlayerEngagement.Shared.Validation;

/// <summary>
/// Helpers for extracting typed values from loosely typed parameter bags.
/// </summary>
public static class ParameterReader {
    /// <summary>
    /// Retrieves a required integer parameter, accepting integral longs/decimals or numeric strings.
    /// Throws when missing or non-integral.
    /// </summary>
    public static int RequireInt(IReadOnlyDictionary<string, object?> parameters, params string[] keys) {
        object? value = FindParameter(parameters, keys);
        return value switch {
            long l when l is >= int.MinValue and <= int.MaxValue => checked((int)l),
            decimal d when decimal.Truncate(d) == d && d is >= int.MinValue and <= int.MaxValue => (int)d,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) => parsed,
            _ => throw new InvalidOperationException($"Parameter '{string.Join("/", keys)}' must be an integer.")
        };
    }

    /// <summary>
    /// Retrieves a required decimal parameter from a number or numeric string.
    /// </summary>
    public static decimal RequireDecimal(IReadOnlyDictionary<string, object?> parameters, params string[] keys) {
        object? value = FindParameter(parameters, keys);
        return value switch {
            long l => l,
            decimal d => d,
            string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) => parsed,
            _ => throw new InvalidOperationException($"Parameter '{string.Join("/", keys)}' must be numeric.")
        };
    }

    /// <summary>
    /// Finds a parameter by any of the provided keys or throws if none exist.
    /// </summary>
    public static object? FindParameter(IReadOnlyDictionary<string, object?> parameters, params string[] keys) {
        foreach (string key in keys) {
            if (parameters.TryGetValue(key, out object? value))
                return value;
        }

        throw new InvalidOperationException($"Parameter '{string.Join("/", keys)}' is required for the streak model.");
    }

    /// <summary>
    /// Retrieves a required array parameter as a list of objects.
    /// </summary>
    public static IReadOnlyList<object?> RequireList(IReadOnlyDictionary<string, object?> parameters, params string[] keys) {
        object? value = FindParameter(parameters, keys);
        if (value is List<object?> list)
            return list;

        throw new InvalidOperationException($"Parameter '{string.Join("/", keys)}' must be an array.");
    }

    /// <summary>
    /// Validates an object is a dictionary and returns it.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> RequireDictionary(object? value, string contextKey) {
        if (value is IReadOnlyDictionary<string, object?> dict)
            return dict;
        if (value is Dictionary<string, object?> d)
            return d;

        throw new InvalidOperationException($"Parameter '{contextKey}' must be an object.");
    }

    /// <summary>
    /// Retrieves a required non-empty string parameter.
    /// </summary>
    public static string RequireString(IReadOnlyDictionary<string, object?> parameters, params string[] keys) {
        object? value = FindParameter(parameters, keys);
        return value switch {
            string s when !string.IsNullOrWhiteSpace(s) => s,
            _ => throw new InvalidOperationException($"Parameter '{string.Join("/", keys)}' must be a non-empty string.")
        };
    }
}
