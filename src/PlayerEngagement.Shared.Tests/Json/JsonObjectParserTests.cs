using System.Collections.Generic;
using PlayerEngagement.Shared.Json;
using Xunit;

namespace PlayerEngagement.Shared.Tests.Json;

public sealed class JsonObjectParserTests {
    [Fact]
    public void ParseObject_EmptyInput_ReturnsEmptyDictionary() {
        Dictionary<string, object?> result = JsonObjectParser.ParseObject(null);

        Assert.Empty(result);
    }

    [Fact]
    public void ParseObject_InvalidJson_ReturnsEmptyDictionary() {
        Dictionary<string, object?> result = JsonObjectParser.ParseObject("{not-json");

        Assert.Empty(result);
    }

    [Fact]
    public void ParseObject_ObjectWithPrimitives_ParsesValues() {
        const string json = """
        {
            "count": 5,
            "name": "alpha",
            "enabled": true,
            "ratio": 1.5,
            "missing": null
        }
        """;

        Dictionary<string, object?> result = JsonObjectParser.ParseObject(json);

        Assert.Equal(5L, result["count"]);
        Assert.Equal("alpha", result["name"]);
        Assert.Equal(true, result["enabled"]);
        Assert.Equal(1.5m, result["ratio"]);
        Assert.Null(result["missing"]);
    }

    [Fact]
    public void ParseObject_ArrayAndNestedObject_ParsesRecursively() {
        const string json = """
        {
            "items": [1, "b", {"flag": false}],
            "child": { "value": 2 }
        }
        """;

        Dictionary<string, object?> result = JsonObjectParser.ParseObject(json);

        List<object?> items = Assert.IsType<List<object?>>(result["items"]);
        Assert.Equal(3, items.Count);
        Assert.Equal(1L, items[0]);
        Assert.Equal("b", items[1]);
        Dictionary<string, object?> nested = Assert.IsType<Dictionary<string, object?>>(items[2]);
        Assert.Equal(false, nested["flag"]);

        Dictionary<string, object?> child = Assert.IsType<Dictionary<string, object?>>(result["child"]);
        Assert.Equal(2L, child["value"]);
    }
}
