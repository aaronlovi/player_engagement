using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Host.Contracts.Policies;
using PlayerEngagement.Host.Validation;
using Xunit;

namespace PlayerEngagement.Host.Tests.Validation;

public sealed class PolicyRequestValidatorTests {
    [Fact]
    public void TryValidateCreate_WithValidPayload_ReturnsTrue() {
        CreatePolicyVersionRequest request = BuildValidCreateRequest();

        bool result = PolicyRequestValidator.TryValidateCreate("daily-login", request, out IDictionary<string, string[]>? errors);

        Assert.True(result);
        Assert.Null(errors);
    }

    [Fact]
    public void TryValidateCreate_WithInvalidPolicyKey_ReturnsError() {
        CreatePolicyVersionRequest request = BuildValidCreateRequest();

        bool result = PolicyRequestValidator.TryValidateCreate("InvalidKey!", request, out IDictionary<string, string[]>? errors);

        Assert.False(result);
        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("policyKey"));
    }

    [Fact]
    public void TryValidateCreate_WithOverlappingSeasonalBoosts_ReturnsError() {
        CreatePolicyVersionRequest request = BuildValidCreateRequest();
        DateTime start = DateTime.UtcNow.AddDays(1);
        request = request with {
            SeasonalBoosts = [
                new(1, "BoostA", 1.2m, start, start.AddDays(2)),
                new(2, "BoostB", 1.3m, start.AddDays(1), start.AddDays(3))
            ]
        };

        bool result = PolicyRequestValidator.TryValidateCreate("daily-login", request, out IDictionary<string, string[]>? errors);

        Assert.False(result);
        Assert.True(errors!.ContainsKey("seasonalBoosts"));
    }

    [Fact]
    public void TryValidatePublish_WithPastEffectiveAt_ReturnsError() {
        PublishPolicyVersionRequest request = new() {
            EffectiveAt = DateTime.UtcNow.AddMinutes(-10)
        };

        bool result = PolicyRequestValidator.TryValidatePublish("daily-login", 1, request, out IDictionary<string, string[]>? errors);

        Assert.False(result);
        Assert.True(errors!.ContainsKey("effectiveAt"));
    }

    [Fact]
    public void TryValidateUpdateSegmentOverrides_WithInvalidSegmentKey_ReturnsError() {
        UpdateSegmentOverridesRequest request = new() {
            Overrides = new Dictionary<string, long> {
                ["invalid-segment!"] = 2
            }
        };

        bool result = PolicyRequestValidator.TryValidateSegmentOverrideUpdate("daily-login", request, out IDictionary<string, string[]>? errors);

        Assert.False(result);
        Assert.True(errors!.ContainsKey("overrides.invalid-segment!"));
    }

    private static CreatePolicyVersionRequest BuildValidCreateRequest() {
        DateTime start = DateTime.UtcNow.AddDays(1);

        return new CreatePolicyVersionRequest {
            DisplayName = "Daily Login XP",
            Description = "Test",
            BaseXpAmount = 100,
            Currency = "XPX",
            ClaimWindowStartMinutes = 60,
            ClaimWindowDurationHours = 8,
            AnchorStrategy = AnchorStrategy.AnchorTimezone.ToString(),
            GraceAllowedMisses = 1,
            GraceWindowDays = 3,
            StreakModelType = StreakModelType.PlateauCap.ToString(),
            StreakModelParameters = new Dictionary<string, object?> { ["capDays"] = 7 },
            PreviewSampleWindowDays = 7,
            PreviewDefaultSegment = "default_segment",
            StreakCurve = [
                new(0, 1.0m, 0, false),
                new(1, 1.2m, 0, false)
            ],
            SeasonalBoosts = [
                new(1, "Boost", 1.5m, start, start.AddDays(1))
            ],
            EffectiveAt = DateTime.UtcNow.AddMinutes(5)
        };
    }
}
