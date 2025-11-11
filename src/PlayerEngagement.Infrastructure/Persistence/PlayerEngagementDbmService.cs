using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Persistence.Migrations;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Infrastructure.Persistence.Statements;

namespace PlayerEngagement.Infrastructure.Persistence;

public sealed class PlayerEngagementDbmService : DbmService, IPlayerEngagementDbmService {
    private readonly ILogger<PlayerEngagementDbmService> _logger;

    public PlayerEngagementDbmService(
        ILoggerFactory loggerFactory,
        PostgresExecutor executor,
        DatabaseOptions options,
        DbMigrations migrations) : base(loggerFactory, executor, options, migrations) {
        _logger = loggerFactory.CreateLogger<PlayerEngagementDbmService>();
    }

    public Task<Result> HealthCheckAsync(CancellationToken ct) {
        _logger.LogInformation("Player Engagement DBM health check requested.");
        return Task.FromResult(Result.Success);
    }

    public async Task<Result<ActivePolicyDTO>> GetCurrentPolicyAsync(string policyKey, DateTime utcNow, CancellationToken ct) {
        var stmt = new GetActivePolicyStmt(SchemaName, policyKey, utcNow);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<ActivePolicyDTO>.Failure(exec);

        if (stmt.ActivePolicy.IsEmpty)
            return Result<ActivePolicyDTO>.Failure(ErrorCodes.NotFound, $"Active policy '{policyKey}' not found.");

        return Result<ActivePolicyDTO>.Success(stmt.ActivePolicy);
    }

    public async Task<Result<PolicyVersionDTO>> GetPolicyVersionAsync(string policyKey, int policyVersion, CancellationToken ct) {
        var stmt = new GetPolicyVersionStmt(SchemaName, policyKey, policyVersion);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<PolicyVersionDTO>.Failure(exec);

        if (stmt.PolicyVersion.IsEmpty)
            return Result<PolicyVersionDTO>.Failure(ErrorCodes.NotFound, $"Policy '{policyKey}' version '{policyVersion}' not found.");

        return Result<PolicyVersionDTO>.Success(stmt.PolicyVersion);
    }

    public async Task<Result<List<PolicyVersionDTO>>> ListPublishedPoliciesAsync(DateTime utcNow, CancellationToken ct) {
        var stmt = new ListPublishedPoliciesStmt(SchemaName, utcNow);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<List<PolicyVersionDTO>>.Failure(exec);

        List<PolicyVersionDTO> copy = new(stmt.Policies.Count);
        copy.AddRange(stmt.Policies);
        return Result<List<PolicyVersionDTO>>.Success(copy);
    }

    public async Task<Result<List<PolicyStreakCurveEntryDTO>>> GetPolicyStreakCurveAsync(string policyKey, int policyVersion, CancellationToken ct) {
        var stmt = new GetPolicyStreakCurveStmt(SchemaName, policyKey, policyVersion);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<List<PolicyStreakCurveEntryDTO>>.Failure(exec);

        List<PolicyStreakCurveEntryDTO> copy = new(stmt.Entries.Count);
        copy.AddRange(stmt.Entries);
        return Result<List<PolicyStreakCurveEntryDTO>>.Success(copy);
    }

    public async Task<Result<List<PolicySeasonalBoostDTO>>> GetPolicySeasonalBoostsAsync(string policyKey, int policyVersion, CancellationToken ct) {
        var stmt = new GetPolicySeasonalBoostsStmt(SchemaName, policyKey, policyVersion);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<List<PolicySeasonalBoostDTO>>.Failure(exec);

        List<PolicySeasonalBoostDTO> copy = new(stmt.Boosts.Count);
        copy.AddRange(stmt.Boosts);
        return Result<List<PolicySeasonalBoostDTO>>.Success(copy);
    }

    public async Task<Result<List<PolicySegmentOverrideDTO>>> GetPolicySegmentOverridesAsync(string policyKey, CancellationToken ct) {
        var stmt = new GetPolicySegmentOverridesStmt(SchemaName, policyKey);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<List<PolicySegmentOverrideDTO>>.Failure(exec);

        List<PolicySegmentOverrideDTO> copy = new(stmt.Overrides.Count);
        copy.AddRange(stmt.Overrides);
        return Result<List<PolicySegmentOverrideDTO>>.Success(copy);
    }
}
