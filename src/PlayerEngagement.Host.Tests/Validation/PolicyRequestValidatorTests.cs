using System;
using System.Collections.Generic;
using PlayerEngagement.Domain.Policies.DailyLoginBonus.XPGrant;
using PlayerEngagement.Host.Contracts.Policies.DailyLoginBonus.XPGrant;
using PlayerEngagement.Host.Validation.DailyLoginBonus.XPGrant;
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

    [Fact]
    public void TryValidateCreate_WithDecayCurveOutOfRange_ReturnsError() {
        CreatePolicyVersionRequest request = BuildValidCreateRequest() with {
            StreakModelType = StreakModelType.DecayCurve.ToString(),
            StreakModelParameters = new Dictionary<string, object?> {
                ["decayPercent"] = 1.5m,
                ["graceDay"] = 0
            }
        };

        bool result = PolicyRequestValidator.TryValidateCreate("daily-login", request, out IDictionary<string, string[]>? errors);

        Assert.False(result);
        Assert.True(errors!.ContainsKey("streakModelParameters.decayPercent"));
    }

    [Fact]
    public void TryValidateCreate_WithOverlappingTiers_ReturnsError() {
        CreatePolicyVersionRequest request = BuildValidCreateRequest() with {
            StreakModelType = StreakModelType.TieredSeasonalReset.ToString(),
            StreakModelParameters = new Dictionary<string, object?> {
                ["tiers"] = new List<object?> {
                    new Dictionary<string, object?> { ["startDay"] = 1, ["endDay"] = 3, ["bonusMultiplier"] = 1.1m },
                    new Dictionary<string, object?> { ["startDay"] = 3, ["endDay"] = 5, ["bonusMultiplier"] = 1.2m }
                }
            }
        };

        bool result = PolicyRequestValidator.TryValidateCreate("daily-login", request, out IDictionary<string, string[]>? errors);

        Assert.False(result);
        Assert.True(errors!.ContainsKey("streakModelParameters.tiers"));
    }

    [Fact]
    public void TryValidateCreate_WithMilestoneMissingFields_ReturnsError() {
        CreatePolicyVersionRequest request = BuildValidCreateRequest() with {
            StreakModelType = StreakModelType.MilestoneMetaReward.ToString(),
            StreakModelParameters = new Dictionary<string, object?> {
                ["milestones"] = new List<object?> {
                    new Dictionary<string, object?> { ["day"] = 2 }
                }
            }
        };

        bool result = PolicyRequestValidator.TryValidateCreate("daily-login", request, out IDictionary<string, string[]>? errors);

        Assert.False(result);
        Assert.True(errors!.ContainsKey("streakModelParameters.milestones[0].rewardType"));
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
            StreakModelParameters = new Dictionary<string, object?> {
                ["plateauDay"] = 7,
                ["plateauMultiplier"] = 1.2m
            },
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
