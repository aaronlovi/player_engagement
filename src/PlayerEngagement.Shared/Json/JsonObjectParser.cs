using System;
using System.Collections.Generic;
using System.Text.Json;

namespace PlayerEngagement.Shared.Json;

/// <summary>
/// Utility methods for safely parsing JSON objects into dictionary and list structures without tying
/// the caller to <see cref="JsonElement"/> lifetimes.
/// </summary>
public static class JsonObjectParser {
    /// <summary>
    /// Parses a JSON object string into a case-insensitive dictionary of <see cref="object"/> values.
    /// Returns an empty dictionary when the payload is null, whitespace, invalid JSON, or not an object.
    /// </summary>
    /// <param name="json">JSON payload expected to represent an object.</param>
    public static Dictionary<string, object?> ParseObject(string? json) {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            return ConvertObject(document.RootElement);
        } catch (JsonException) {
            return [];
        }
    }

    /// <summary>
    /// Converts a <see cref="JsonElement"/> into a CLR object graph composed of dictionaries, lists,
    /// and primitive values. Intended for callers that already have a parsed element.
    /// </summary>
    /// <param name="element">Element to convert.</param>
    public static object? ConvertElement(JsonElement element) => element.ValueKind switch {
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => ConvertNumber(element),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Array => ConvertArray(element),
        JsonValueKind.Object => ConvertObject(element),
        _ => null
    };

    private static object ConvertNumber(JsonElement element) {
        if (element.TryGetInt64(out long intValue))
            return intValue;

        decimal decimalValue = element.GetDecimal();
        if (decimal.Truncate(decimalValue) == decimalValue)
            return (long)decimalValue;

        return decimalValue;
    }

    private static List<object?> ConvertArray(JsonElement element) {
        List<object?> items = new(element.GetArrayLength());
        foreach (JsonElement child in element.EnumerateArray())
            items.Add(ConvertElement(child));

        return items;
    }

    private static Dictionary<string, object?> ConvertObject(JsonElement element) {
        Dictionary<string, object?> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in element.EnumerateObject())
            result[property.Name] = ConvertElement(property.Value);

        return result;
    }
}
