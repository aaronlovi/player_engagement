using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PlayerEngagement.Domain.Policies;
using PlayerEngagement.Infrastructure.Persistence;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Infrastructure.Policies.Services;
using PlayerEngagement.Infrastructure.Tests.TestUtilities;
using Xunit;

namespace PlayerEngagement.Infrastructure.Tests.Policies.Services;

public sealed class PolicyDocumentPersistenceServiceTests : IDisposable {
    private readonly Mock<IPlayerEngagementDbmService> _dbmMock = new(MockBehavior.Strict);
    private readonly PolicyDocumentPersistenceService _service;

    public PolicyDocumentPersistenceServiceTests() {
        _service = new PolicyDocumentPersistenceService(_dbmMock.Object, NullLoggerFactory.Instance);
    }

    public void Dispose() => _dbmMock.VerifyAll();

    [Fact]
    public async Task GetCurrentPolicyAsync_ReturnsMappedDocument() {
        ActivePolicyDTO active = PolicyDtoFactory.CreateActive("daily-login", version: 2);
        List<PolicyStreakCurveEntryDTO> streak = [new(1, active.PolicyKey, active.PolicyVersion, 1, 1.1m, 5, false)];
        List<PolicySeasonalBoostDTO> boosts = [
            new(1, active.PolicyKey, active.PolicyVersion, "Boost", 2.0m, DateTime.UtcNow, DateTime.UtcNow.AddDays(1))
        ];

        _ = _dbmMock.Setup(dbm => dbm.GetCurrentPolicyAsync(active.PolicyKey, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivePolicyDTO>.Success(active));
        _ = _dbmMock.Setup(dbm => dbm.GetPolicyStreakCurveAsync(active.PolicyKey, active.PolicyVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PolicyStreakCurveEntryDTO>>.Success(streak));
        _ = _dbmMock.Setup(dbm => dbm.GetPolicySeasonalBoostsAsync(active.PolicyKey, active.PolicyVersion, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PolicySeasonalBoostDTO>>.Success(boosts));

        PolicyDocument? document = await _service.GetCurrentPolicyAsync(active.PolicyKey, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.NotNull(document);
        Assert.Equal(2, document!.Version.PolicyVersion);
        _ = Assert.Single(document.StreakCurve);
        _ = Assert.Single(document.SeasonalBoosts);
    }

    [Fact]
    public async Task GetCurrentPolicyAsync_ReturnsNull_WhenDbmFails() {
        string policyKey = "missing";
        _ = _dbmMock.Setup(dbm => dbm.GetCurrentPolicyAsync(policyKey, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivePolicyDTO>.Success(ActivePolicyDTO.Empty));

        PolicyDocument? document = await _service.GetCurrentPolicyAsync(policyKey, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Null(document);
    }

    [Fact]
    public async Task ListPublishedPoliciesAsync_MapsEachVersion() {
        List<PolicyVersionDTO> versions = [
            PolicyDtoFactory.CreateVersion("daily-login", version: 1),
            PolicyDtoFactory.CreateVersion("daily-login", version: 2)
        ];
        _ = _dbmMock.Setup(dbm => dbm.ListPublishedPoliciesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PolicyVersionDTO>>.Success(versions));
        _ = _dbmMock.Setup(dbm => dbm.GetPolicyStreakCurveAsync("daily-login", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PolicyStreakCurveEntryDTO>>.Success(new List<PolicyStreakCurveEntryDTO>()));
        _ = _dbmMock.Setup(dbm => dbm.GetPolicySeasonalBoostsAsync("daily-login", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PolicySeasonalBoostDTO>>.Success(new List<PolicySeasonalBoostDTO>()));
        _ = _dbmMock.Setup(dbm => dbm.GetPolicyStreakCurveAsync("daily-login", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PolicyStreakCurveEntryDTO>>.Success(new List<PolicyStreakCurveEntryDTO>()));
        _ = _dbmMock.Setup(dbm => dbm.GetPolicySeasonalBoostsAsync("daily-login", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PolicySeasonalBoostDTO>>.Success(new List<PolicySeasonalBoostDTO>()));

        IReadOnlyList<PolicyDocument> documents = await _service.ListPublishedPoliciesAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(2, documents.Count);
    }

    [Fact]
    public async Task GetSegmentOverridesAsync_ReturnsEmptyDictionaryOnFailure() {
        _ = _dbmMock.Setup(dbm => dbm.GetPolicySegmentOverridesAsync("daily-login", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<PolicySegmentOverrideDTO>>.Success(null!));

        IReadOnlyDictionary<string, int> overrides = await _service.GetSegmentOverridesAsync("daily-login", CancellationToken.None);

        Assert.Empty(overrides);
    }
}
