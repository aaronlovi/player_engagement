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
}
