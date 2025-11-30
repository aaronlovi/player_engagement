using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using PlayerEngagement.Infrastructure.Persistence;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Infrastructure.Tests.TestUtilities;
using Xunit;

namespace PlayerEngagement.Infrastructure.Tests.Persistence;

public sealed class PlayerEngagementDbmInMemoryServiceTests {
    private readonly PlayerEngagementDbmInMemoryService _service = new(NullLoggerFactory.Instance);

    [Fact]
    public async Task GetCurrentPolicyAsync_ReturnsValue_WhenPolicyExists() {
        string policyKey = Guid.NewGuid().ToString();
        ActivePolicyDTO dto = PolicyDtoFactory.CreateActive(policyKey, version: 5);
        PlayerEngagementDbmInMemoryData.UpsertActivePolicy(dto);

        Result<ActivePolicyDTO> result = await _service.GetCurrentPolicyAsync(policyKey, DateTime.UtcNow, CancellationToken.None);

        Assert.False(result.IsFailure);
        Assert.Equal(5, result.Value!.PolicyVersion);
    }

    [Fact]
    public async Task GetPolicyVersionAsync_ReturnsFailure_WhenVersionMissing() {
        string policyKey = Guid.NewGuid().ToString();

        Result<PolicyVersionDTO> result = await _service.GetPolicyVersionAsync(policyKey, 99, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ListPublishedPoliciesAsync_FiltersByStatusAndEffectiveWindow() {
        string policyKey = Guid.NewGuid().ToString();
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 1, status: "Published", effectiveAt: DateTime.UtcNow.AddHours(-1)));
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 2, status: "Draft"));
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 3, status: "Published", effectiveAt: DateTime.UtcNow.AddHours(1)));

        Result<List<PolicyVersionDTO>> result = await _service.ListPublishedPoliciesAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.False(result.IsFailure);
        _ = Assert.Single(result.Value!, v => v.PolicyVersion == 1);
    }

    [Fact]
    public async Task GetPolicySegmentOverridesAsync_ReturnsSnapshot() {
        string policyKey = Guid.NewGuid().ToString();
        List<PolicySegmentOverrideDTO> overrides = [
            new(1, "vip", policyKey, 5, DateTime.UtcNow, "agent")
        ];
        PlayerEngagementDbmInMemoryData.SetSegmentOverrides(policyKey, overrides);

        Result<List<PolicySegmentOverrideDTO>> result = await _service.GetPolicySegmentOverridesAsync(policyKey, CancellationToken.None);

        Assert.False(result.IsFailure);
        _ = Assert.Single(result.Value!, o => o.TargetPolicyVersion == 5);
    }

    [Fact]
    public async Task GetPolicyStreakCurveAsync_ReturnsDeepCopy() {
        string policyKey = Guid.NewGuid().ToString();
        long version = 7;
        List<PolicyStreakCurveEntryDTO> entries = [
            new(1, policyKey, version, 1, 1.1m, 10, false)
        ];
        PlayerEngagementDbmInMemoryData.SetStreakCurve(policyKey, version, entries);

        Result<List<PolicyStreakCurveEntryDTO>> result = await _service.GetPolicyStreakCurveAsync(policyKey, version, CancellationToken.None);

        Assert.False(result.IsFailure);
        Assert.Equal(1, result.Value![0].DayIndex);

        // mutate source - should not affect snapshot
        entries[0] = new PolicyStreakCurveEntryDTO(2, policyKey, version, 2, 2.0m, 20, true);
        Assert.Equal(1, result.Value![0].DayIndex);
    }

    [Fact]
    public async Task GetPolicySeasonalBoostsAsync_ReturnsDeepCopy() {
        string policyKey = Guid.NewGuid().ToString();
        long version = 4;
        List<PolicySeasonalBoostDTO> boosts = [
            new(1, policyKey, version, "spring", 1.5m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1))
        ];
        PlayerEngagementDbmInMemoryData.SetSeasonalBoosts(policyKey, version, boosts);

        Result<List<PolicySeasonalBoostDTO>> result = await _service.GetPolicySeasonalBoostsAsync(policyKey, version, CancellationToken.None);

        Assert.False(result.IsFailure);
        Assert.Equal("spring", result.Value![0].Label);

        boosts[0] = new PolicySeasonalBoostDTO(2, policyKey, version, "summer", 2.0m, DateTime.UtcNow, DateTime.UtcNow.AddDays(2));
        Assert.Equal("spring", result.Value![0].Label);
    }

    [Fact]
    public async Task CreatePolicyDraftAsync_CreatesVersion() {
        string policyKey = Guid.NewGuid().ToString();
        long policyId = 1001;
        long versionId = 2001;
        PolicyVersionWriteDto dto = new(
            policyKey,
            "Daily Login",
            "desc",
            100,
            "XPX",
            60,
            6,
            "ANCHOR_TIMEZONE",
            1,
            3,
            "PLATEAU_CAP",
            "{\"cap\":7}",
            7,
            "default",
            "{}",
            null,
            DateTime.UtcNow,
            "tester",
            versionId,
            policyId);

        List<PolicyStreakCurveEntryDTO> streak = [
            new(0, policyKey, 0, 0, 1.0m, 0, false)
        ];
        List<PolicySeasonalBoostDTO> boosts = [
            new(0, policyKey, 0, "spring", 1.25m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1))
        ];

        Result<long> result = await _service.CreatePolicyDraftAsync(dto, streak, boosts, CancellationToken.None);

        Assert.False(result.IsFailure);
        Result<PolicyVersionDTO> stored = await _service.GetPolicyVersionAsync(policyKey, versionId, CancellationToken.None);
        Assert.False(stored.IsFailure);
        Assert.Equal(versionId, stored.Value!.PolicyVersion);
    }

    [Fact]
    public async Task CreatePolicyDraftAsync_Fails_WhenVersionAlreadyExists() {
        string policyKey = Guid.NewGuid().ToString();
        long versionId = 3001;
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: versionId, status: "Draft"));

        PolicyVersionWriteDto dto = new(
            policyKey,
            "Daily Login",
            "desc",
            100,
            "XPX",
            60,
            6,
            "ANCHOR_TIMEZONE",
            1,
            3,
            "PLATEAU_CAP",
            "{\"cap\":7}",
            7,
            "default",
            "{}",
            null,
            DateTime.UtcNow,
            "tester",
            versionId,
            999);

        Result<long> result = await _service.CreatePolicyDraftAsync(dto, Array.Empty<PolicyStreakCurveEntryDTO>(), Array.Empty<PolicySeasonalBoostDTO>(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task PublishPolicyVersionAsync_ArchivesExistingPublished() {
        string policyKey = Guid.NewGuid().ToString();
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 1, status: "Published"));
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 2, status: "Draft"));

        Result<PolicyVersionDTO> result = await _service.PublishPolicyVersionAsync(
            policyKey,
            2,
            DateTime.UtcNow,
            DateTime.UtcNow,
            Array.Empty<PolicySegmentOverrideDTO>(),
            CancellationToken.None);

        Assert.False(result.IsFailure);

        Result<PolicyVersionDTO> published = await _service.GetPolicyVersionAsync(policyKey, 2, CancellationToken.None);
        Result<PolicyVersionDTO> archived = await _service.GetPolicyVersionAsync(policyKey, 1, CancellationToken.None);

        Assert.Equal("Published", published.Value!.Status);
        Assert.Equal("Archived", archived.Value!.Status);
    }

    [Fact]
    public async Task RetirePolicyVersionAsync_SetsArchivedStatus() {
        string policyKey = Guid.NewGuid().ToString();
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 5, status: "Published"));

        Result<PolicyVersionDTO> result = await _service.RetirePolicyVersionAsync(policyKey, 5, DateTime.UtcNow, CancellationToken.None);

        Assert.False(result.IsFailure);
        Assert.Equal("Archived", result.Value!.Status);
    }

    [Fact]
    public async Task ListPolicyVersionsAsync_FiltersByStatus() {
        string policyKey = Guid.NewGuid().ToString();
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 1, status: "Draft"));
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 2, status: "Published"));
        PlayerEngagementDbmInMemoryData.UpsertPolicyVersion(PolicyDtoFactory.CreateVersion(policyKey, version: 3, status: "Archived"));

        Result<List<PolicyVersionDTO>> result = await _service.ListPolicyVersionsAsync(policyKey, "Published", null, null, CancellationToken.None);

        Assert.False(result.IsFailure);
        Assert.Single(result.Value!);
        Assert.Equal(2, result.Value![0].PolicyVersion);
    }

    [Fact]
    public async Task UpsertPolicySegmentOverridesAsync_ReplacesExisting() {
        string policyKey = Guid.NewGuid().ToString();
        List<PolicySegmentOverrideDTO> initial = [
            new(0, "vip", policyKey, 5, DateTime.UtcNow.AddMinutes(-5), "initial")
        ];
        _ = await _service.UpsertPolicySegmentOverridesAsync(policyKey, initial, CancellationToken.None);

        List<PolicySegmentOverrideDTO> replacement = [
            new(0, "vip", policyKey, 6, DateTime.UtcNow, "updated"),
            new(0, "standard", policyKey, 5, DateTime.UtcNow, "updated")
        ];

        _ = await _service.UpsertPolicySegmentOverridesAsync(policyKey, replacement, CancellationToken.None);
        Result<List<PolicySegmentOverrideDTO>> overrides = await _service.GetPolicySegmentOverridesAsync(policyKey, CancellationToken.None);

        Assert.False(overrides.IsFailure);
        Assert.Equal(2, overrides.Value!.Count);
        Assert.Contains(overrides.Value!, o => o.TargetPolicyVersion == 6 && o.SegmentKey == "vip");
    }
}
