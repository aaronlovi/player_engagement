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
}
