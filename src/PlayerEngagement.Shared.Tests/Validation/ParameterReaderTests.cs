using System;
using System.Collections.Generic;
using PlayerEngagement.Shared.Validation;
using Xunit;

namespace PlayerEngagement.Shared.Tests.Validation;

public sealed class ParameterReaderTests {
    [Fact]
    public void RequireInt_AllowsIntegralDecimal() {
        Dictionary<string, object?> parameters = new(StringComparer.OrdinalIgnoreCase) {
            ["value"] = 5.0m
        };

        int result = ParameterReader.RequireInt(parameters, "value");

        Assert.Equal(5, result);
    }

    [Fact]
    public void RequireInt_ThrowsWhenMissing() => Assert.Throws<InvalidOperationException>(() => ParameterReader.RequireInt(new Dictionary<string, object?>(), "missing"));

    [Fact]
    public void RequireDecimal_ParsesNumericString() {
        Dictionary<string, object?> parameters = new(StringComparer.OrdinalIgnoreCase) {
            ["value"] = "1.25"
        };

        decimal result = ParameterReader.RequireDecimal(parameters, "value");

        Assert.Equal(1.25m, result);
    }

    [Fact]
    public void FindParameter_RespectsAlternateKeys() {
        Dictionary<string, object?> parameters = new(StringComparer.OrdinalIgnoreCase) {
            ["first"] = 1L
        };

        object? result = ParameterReader.FindParameter(parameters, "missing", "first");

        Assert.Equal(1L, result);
    }

    [Fact]
    public void RequireList_ReturnsList() {
        List<object?> items = [1, 2, 3];
        Dictionary<string, object?> parameters = new(StringComparer.OrdinalIgnoreCase) {
            ["arr"] = items
        };

        IReadOnlyList<object?> result = ParameterReader.RequireList(parameters, "arr");

        Assert.Same(items, result);
    }

    [Fact]
    public void RequireDictionary_ReturnsDictionary() {
        Dictionary<string, object?> dict = new(StringComparer.OrdinalIgnoreCase) {
            ["a"] = 1
        };

        IReadOnlyDictionary<string, object?> result = ParameterReader.RequireDictionary(dict, "dict");

        Assert.Same(dict, result);
    }

    [Fact]
    public void RequireString_Valid() {
        Dictionary<string, object?> parameters = new(StringComparer.OrdinalIgnoreCase) {
            ["name"] = "foo"
        };

        string result = ParameterReader.RequireString(parameters, "name");

        Assert.Equal("foo", result);
    }
}
