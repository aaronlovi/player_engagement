using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Persistence.Migrations;
using Npgsql;
using InnoAndLogic.Shared;
using InnoAndLogic.Shared.Models;
using Microsoft.Extensions.Logging;
using PlayerEngagement.Infrastructure.Persistence.DTOs.DailyLoginBonus.XPGrant;
using PlayerEngagement.Infrastructure.Persistence.DTOs.Seasons;
using PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

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
        _logger.LogInformation("HealthCheckAsync invoked.");
        return Task.FromResult(Result.Success);
    }

    public async Task<Result<long>> CreatePolicyDraftAsync(
        PolicyVersionWriteDto dto,
        IReadOnlyList<PolicyStreakCurveEntryDTO> streak,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct) {

        _logger.LogInformation(
            "CreatePolicyDraftAsync start: policyKey={PolicyKey}, requestedVersion={RequestedVersion}, streakCount={StreakCount}, boostCount={BoostCount}",
            dto.PolicyKey,
            dto.PolicyVersion,
            streak.Count,
            boosts.Count);

        if (dto is null) {
            _logger.LogError("CreatePolicyDraftAsync failed: dto is null");
            return Result<long>.Failure(ErrorCodes.ValidationError, "Request payload is required.");
        }

        if (streak is null) {
            _logger.LogError("CreatePolicyDraftAsync failed: streak collection is null");
            return Result<long>.Failure(ErrorCodes.ValidationError, "Streak curve entries are required.");
        }

        if (boosts is null) {
            _logger.LogError("CreatePolicyDraftAsync failed: seasonal boosts collection is null");
            return Result<long>.Failure(ErrorCodes.ValidationError, "Seasonal boosts are required (empty allowed).");
        }

        long policyVersion = dto.PolicyVersion != 0 ? dto.PolicyVersion : (long)await GetNextId64(ct);
        long policyId = dto.PolicyId ?? (long)await GetNextId64(ct);
        _logger.LogInformation("Assigned ids for create: policyVersion={PolicyVersion}, policyId={PolicyId}", policyVersion, policyId);

        List<PolicyStreakCurveEntryDTO> normalizedStreak = await NormalizeStreakEntriesAsync(dto.PolicyKey, policyVersion, streak, ct);
        List<PolicySeasonalBoostDTO> normalizedBoosts = await NormalizeSeasonalBoostsAsync(dto.PolicyKey, policyVersion, boosts, ct);

        using TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx = await Executor.BeginTransaction(ct);

        var ensurePolicyStmt = new EnsurePolicyShellStmt(
            SchemaName,
            dto.PolicyKey,
            policyId,
            dto.DisplayName,
            dto.Description ?? string.Empty,
            dto.CreatedAt,
            dto.CreatedBy);

        _logger.LogInformation("Executing EnsurePolicyShellStmt for policyKey={PolicyKey}", dto.PolicyKey);
        Result ensureResult = await Executor.ExecuteUnderTransactionWithRetry(ensurePolicyStmt, tx, ct);
        if (ensureResult.IsFailure) {
            tx.Rollback();
            _logger.LogError("EnsurePolicyShellStmt failed for policyKey={PolicyKey}: {Error}", dto.PolicyKey, ensureResult.ErrorMessage);
            return Result<long>.Failure(ensureResult);
        }
        _logger.LogInformation(
            "EnsurePolicyShellStmt succeeded for policyKey={PolicyKey}, resolvedPolicyId={PolicyId}",
            dto.PolicyKey,
            ensurePolicyStmt.PolicyId);

        policyId = ensurePolicyStmt.PolicyId;

        var insertVersionStmt = new InsertPolicyVersionStmt(
            SchemaName,
            dto.PolicyKey,
            policyVersion,
            "Draft",
            dto.BaseXpAmount,
            dto.Currency,
            dto.ClaimWindowStartMinutes,
            dto.ClaimWindowDurationHours,
            dto.AnchorStrategy,
            dto.GraceAllowedMisses,
            dto.GraceWindowDays,
            dto.StreakModelType,
            dto.StreakModelParameters,
            dto.PreviewSampleWindowDays,
            dto.PreviewDefaultSegment,
            dto.SeasonalMetadata,
            dto.EffectiveAt,
            null,
            dto.CreatedAt,
            dto.CreatedBy,
            null);

        _logger.LogInformation("Executing InsertPolicyVersionStmt for policyKey={PolicyKey}, version={Version}", dto.PolicyKey, policyVersion);
        Result versionResult = await Executor.ExecuteUnderTransactionWithRetry(insertVersionStmt, tx, ct);
        if (versionResult.IsFailure) {
            tx.Rollback();
            _logger.LogError("InsertPolicyVersionStmt failed for policyKey={PolicyKey}, version={Version}: {Error}", dto.PolicyKey, policyVersion, versionResult.ErrorMessage);
            return Result<long>.Failure(versionResult);
        }
        _logger.LogInformation(
            "InsertPolicyVersionStmt succeeded for policyKey={PolicyKey}, version={Version}, rowsAffected={Rows}",
            dto.PolicyKey,
            policyVersion,
            insertVersionStmt.NumRowsAffected);

        _logger.LogInformation("Replacing streak curve for policyKey={PolicyKey}, version={Version}", dto.PolicyKey, policyVersion);
        Result streakResult = await ReplaceStreakCurveAsync(dto.PolicyKey, policyVersion, normalizedStreak, tx, ct);
        if (streakResult.IsFailure) {
            tx.Rollback();
            _logger.LogError("ReplaceStreakCurve failed for policyKey={PolicyKey}, version={Version}: {Error}", dto.PolicyKey, policyVersion, streakResult.ErrorMessage);
            return Result<long>.Failure(streakResult);
        }
        _logger.LogInformation("ReplaceStreakCurve succeeded for policyKey={PolicyKey}, version={Version}", dto.PolicyKey, policyVersion);

        _logger.LogInformation("Replacing seasonal boosts for policyKey={PolicyKey}, version={Version}", dto.PolicyKey, policyVersion);
        Result boostResult = await ReplaceSeasonalBoostsAsync(dto.PolicyKey, policyVersion, normalizedBoosts, tx, ct);
        if (boostResult.IsFailure) {
            tx.Rollback();
            _logger.LogError("ReplaceSeasonalBoosts failed for policyKey={PolicyKey}, version={Version}: {Error}", dto.PolicyKey, policyVersion, boostResult.ErrorMessage);
            return Result<long>.Failure(boostResult);
        }
        _logger.LogInformation("ReplaceSeasonalBoosts succeeded for policyKey={PolicyKey}, version={Version}", dto.PolicyKey, policyVersion);

        tx.Commit();
        _logger.LogInformation("CreatePolicyDraftAsync complete: policyKey={PolicyKey}, version={Version}", dto.PolicyKey, policyVersion);
        return Result<long>.Success(policyVersion);
    }

    public async Task<Result<PolicyVersionDTO>> PublishPolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime publishedAt,
        DateTime? effectiveAt,
        IReadOnlyList<PolicySegmentOverrideDTO> segmentOverrides,
        CancellationToken ct) {

        _logger.LogInformation(
            "PublishPolicyVersionAsync start: policyKey={PolicyKey}, version={Version}, effectiveAt={EffectiveAt}, overrides={OverrideCount}",
            policyKey,
            policyVersion,
            effectiveAt,
            segmentOverrides.Count);

        if (segmentOverrides is null) {
            _logger.LogError("PublishPolicyVersionAsync failed: segmentOverrides is null for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
            return Result<PolicyVersionDTO>.Failure(ErrorCodes.ValidationError, "Segment overrides collection is required.");
        }

        List<PolicySegmentOverrideDTO> overrides = await NormalizeSegmentOverridesAsync(policyKey, segmentOverrides, ct);

        using TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx = await Executor.BeginTransaction(ct);

        var archiveStmt = new ArchiveCurrentPublishedStmt(SchemaName, policyKey, effectiveAt ?? publishedAt);
        _logger.LogInformation("Executing ArchiveCurrentPublishedStmt for policyKey={PolicyKey}", policyKey);
        Result archiveResult = await Executor.ExecuteUnderTransactionWithRetry(archiveStmt, tx, ct);
        if (archiveResult.IsFailure) {
            tx.Rollback();
            _logger.LogError("ArchiveCurrentPublishedStmt failed for policyKey={PolicyKey}: {Error}", policyKey, archiveResult.ErrorMessage);
            return Result<PolicyVersionDTO>.Failure(archiveResult);
        }
        _logger.LogInformation("ArchiveCurrentPublishedStmt succeeded for policyKey={PolicyKey}", policyKey);

        var publishStmt = new PublishPolicyVersionStmt(SchemaName, policyKey, policyVersion, effectiveAt, publishedAt);
        _logger.LogInformation("Executing PublishPolicyVersionStmt for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        Result publishResult = await Executor.ExecuteUnderTransactionWithRetry(publishStmt, tx, ct);
        if (publishResult.IsFailure || publishStmt.NumRowsAffected == 0) {
            tx.Rollback();
            _logger.LogError("PublishPolicyVersionStmt failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, publishResult.ErrorMessage);
            return Result<PolicyVersionDTO>.Failure(ErrorCodes.NotFound, $"Policy '{policyKey}' version '{policyVersion}' not found or already published.");
        }
        _logger.LogInformation("PublishPolicyVersionStmt succeeded for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);

        _logger.LogInformation("Replacing segment overrides for policyKey={PolicyKey}", policyKey);
        Result overrideResult = await ReplaceSegmentOverridesAsync(policyKey, overrides, tx, ct);
        if (overrideResult.IsFailure) {
            tx.Rollback();
            _logger.LogError("ReplaceSegmentOverrides failed for policyKey={PolicyKey}: {Error}", policyKey, overrideResult.ErrorMessage);
            return Result<PolicyVersionDTO>.Failure(overrideResult);
        }
        _logger.LogInformation("ReplaceSegmentOverrides succeeded for policyKey={PolicyKey}", policyKey);

        tx.Commit();

        Result<PolicyVersionDTO> loaded = await GetPolicyVersionAsync(policyKey, policyVersion, ct);
        if (loaded.IsFailure)
            return loaded;

        _logger.LogInformation("PublishPolicyVersionAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return Result<PolicyVersionDTO>.Success(loaded.Value!);
    }

    public async Task<Result<PolicyVersionDTO>> RetirePolicyVersionAsync(
        string policyKey,
        long policyVersion,
        DateTime retiredAt,
        CancellationToken ct) {

        _logger.LogInformation("RetirePolicyVersionAsync start: policyKey={PolicyKey}, version={Version}, retiredAt={RetiredAt}", policyKey, policyVersion, retiredAt);
        using TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx = await Executor.BeginTransaction(ct);

        var retireStmt = new RetirePolicyVersionStmt(SchemaName, policyKey, policyVersion, retiredAt);
        _logger.LogInformation("Executing RetirePolicyVersionStmt for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        Result retireResult = await Executor.ExecuteUnderTransactionWithRetry(retireStmt, tx, ct);
        if (retireResult.IsFailure || retireStmt.NumRowsAffected == 0) {
            tx.Rollback();
            _logger.LogError("RetirePolicyVersionStmt failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, retireResult.ErrorMessage);
            return Result<PolicyVersionDTO>.Failure(ErrorCodes.NotFound, $"Policy '{policyKey}' version '{policyVersion}' not found or not published.");
        }
        _logger.LogInformation("RetirePolicyVersionStmt succeeded for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);

        tx.Commit();

        Result<PolicyVersionDTO> loaded = await GetPolicyVersionAsync(policyKey, policyVersion, ct);
        _logger.LogInformation("RetirePolicyVersionAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return loaded;
    }

    public async Task<Result> ReplacePolicyStreakCurveAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicyStreakCurveEntryDTO> entries,
        CancellationToken ct) {

        _logger.LogInformation("ReplacePolicyStreakCurveAsync start: policyKey={PolicyKey}, version={Version}, count={Count}", policyKey, policyVersion, entries.Count);
        List<PolicyStreakCurveEntryDTO> normalized = await NormalizeStreakEntriesAsync(policyKey, policyVersion, entries, ct);
        using TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx = await Executor.BeginTransaction(ct);

        Result replaceResult = await ReplaceStreakCurveAsync(policyKey, policyVersion, normalized, tx, ct);
        if (replaceResult.IsFailure) {
            tx.Rollback();
            _logger.LogError("ReplacePolicyStreakCurveAsync failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, replaceResult.ErrorMessage);
            return replaceResult;
        }

        tx.Commit();
        _logger.LogInformation("ReplacePolicyStreakCurveAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return Result.Success;
    }

    public async Task<Result> ReplacePolicySeasonalBoostsAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct) {

        _logger.LogInformation("ReplacePolicySeasonalBoostsAsync start: policyKey={PolicyKey}, version={Version}, count={Count}", policyKey, policyVersion, boosts.Count);
        List<PolicySeasonalBoostDTO> normalized = await NormalizeSeasonalBoostsAsync(policyKey, policyVersion, boosts, ct);
        using TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx = await Executor.BeginTransaction(ct);

        Result replaceResult = await ReplaceSeasonalBoostsAsync(policyKey, policyVersion, normalized, tx, ct);
        if (replaceResult.IsFailure) {
            tx.Rollback();
            _logger.LogError("ReplacePolicySeasonalBoostsAsync failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, replaceResult.ErrorMessage);
            return replaceResult;
        }

        tx.Commit();
        _logger.LogInformation("ReplacePolicySeasonalBoostsAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return Result.Success;
    }

    public async Task<Result> UpsertPolicySegmentOverridesAsync(
        string policyKey,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides,
        CancellationToken ct) {

        _logger.LogInformation("UpsertPolicySegmentOverridesAsync start: policyKey={PolicyKey}, count={Count}", policyKey, overrides.Count);
        if (overrides is null) {
            _logger.LogError("UpsertPolicySegmentOverridesAsync failed: overrides is null for policyKey={PolicyKey}", policyKey);
            return Result.Failure(ErrorCodes.ValidationError, "Overrides collection is required.");
        }
        List<PolicySegmentOverrideDTO> normalized = await NormalizeSegmentOverridesAsync(policyKey, overrides, ct);

        using TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx = await Executor.BeginTransaction(ct);
        Result result = await ReplaceSegmentOverridesAsync(policyKey, normalized, tx, ct);
        if (result.IsFailure) {
            tx.Rollback();
            _logger.LogError("UpsertPolicySegmentOverridesAsync failed for policyKey={PolicyKey}: {Error}", policyKey, result.ErrorMessage);
            return result;
        }

        tx.Commit();
        _logger.LogInformation("UpsertPolicySegmentOverridesAsync complete: policyKey={PolicyKey}", policyKey);
        return Result.Success;
    }

    public async Task<Result<List<PolicyVersionDTO>>> ListPolicyVersionsAsync(string policyKey, string? status, DateTime? effectiveBefore, int? limit, CancellationToken ct) {
        _logger.LogInformation("ListPolicyVersionsAsync start: policyKey={PolicyKey}, status={Status}, effectiveBefore={EffectiveBefore}, limit={Limit}", policyKey, status, effectiveBefore, limit);
        int limited = limit.HasValue && limit.Value > 0 ? limit.Value : 200;
        var stmt = new ListPolicyVersionsStmt(SchemaName, policyKey, status, effectiveBefore, limited);
        _logger.LogInformation("Executing ListPolicyVersionsStmt for policyKey={PolicyKey}", policyKey);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure) {
            _logger.LogError("ListPolicyVersionsStmt failed for policyKey={PolicyKey}: {Error}", policyKey, exec.ErrorMessage);
            return Result<List<PolicyVersionDTO>>.Failure(exec);
        }

        List<PolicyVersionDTO> copy = [.. stmt.Versions];
        _logger.LogInformation("ListPolicyVersionsAsync complete: policyKey={PolicyKey}, returned={Count}", policyKey, copy.Count);
        return Result<List<PolicyVersionDTO>>.Success(copy);
    }

    public async Task<Result<ActivePolicyDTO>> GetCurrentPolicyAsync(string policyKey, DateTime utcNow, CancellationToken ct) {
        _logger.LogInformation("GetCurrentPolicyAsync start: policyKey={PolicyKey}, utcNow={UtcNow}", policyKey, utcNow);
        var stmt = new GetActivePolicyStmt(SchemaName, policyKey, utcNow);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<ActivePolicyDTO>.Failure(exec);

        if (stmt.ActivePolicy.IsEmpty)
            return Result<ActivePolicyDTO>.Failure(ErrorCodes.NotFound, $"Active policy '{policyKey}' not found.");

        _logger.LogInformation("GetCurrentPolicyAsync complete: policyKey={PolicyKey}", policyKey);
        return Result<ActivePolicyDTO>.Success(stmt.ActivePolicy);
    }

    public async Task<Result<PolicyVersionDTO>> GetPolicyVersionAsync(string policyKey, long policyVersion, CancellationToken ct) {
        _logger.LogInformation("GetPolicyVersionAsync start: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        var stmt = new GetPolicyVersionStmt(SchemaName, policyKey, policyVersion);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure) {
            _logger.LogError("GetPolicyVersionStmt failed: policyKey={PolicyKey}, version={Version}, error={Error}", policyKey, policyVersion, exec.ErrorMessage);
            return Result<PolicyVersionDTO>.Failure(exec);
        }

        if (stmt.PolicyVersion.IsEmpty) {
            _logger.LogWarning("GetPolicyVersionAsync not found: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
            return Result<PolicyVersionDTO>.Failure(ErrorCodes.NotFound, $"Policy '{policyKey}' version '{policyVersion}' not found.");
        }

        _logger.LogInformation("GetPolicyVersionAsync complete: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        return Result<PolicyVersionDTO>.Success(stmt.PolicyVersion);
    }

    public async Task<Result<List<PolicyVersionDTO>>> ListPublishedPoliciesAsync(DateTime utcNow, CancellationToken ct) {
        _logger.LogInformation("ListPublishedPoliciesAsync start: utcNow={UtcNow}", utcNow);
        var stmt = new ListPublishedPoliciesStmt(SchemaName, utcNow);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<List<PolicyVersionDTO>>.Failure(exec);

        List<PolicyVersionDTO> copy = [.. stmt.Policies];
        _logger.LogInformation("ListPublishedPoliciesAsync complete: count={Count}", copy.Count);
        return Result<List<PolicyVersionDTO>>.Success(copy);
    }

    public async Task<Result<List<PolicyStreakCurveEntryDTO>>> GetPolicyStreakCurveAsync(string policyKey, long policyVersion, CancellationToken ct) {
        _logger.LogInformation("GetPolicyStreakCurveAsync start: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        var stmt = new GetPolicyStreakCurveStmt(SchemaName, policyKey, policyVersion);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<List<PolicyStreakCurveEntryDTO>>.Failure(exec);

        List<PolicyStreakCurveEntryDTO> copy = [.. stmt.Entries];
        _logger.LogInformation("GetPolicyStreakCurveAsync complete: policyKey={PolicyKey}, version={Version}, count={Count}", policyKey, policyVersion, copy.Count);
        return Result<List<PolicyStreakCurveEntryDTO>>.Success(copy);
    }

    public async Task<Result<List<PolicySeasonalBoostDTO>>> GetPolicySeasonalBoostsAsync(string policyKey, long policyVersion, CancellationToken ct) {
        _logger.LogInformation("GetPolicySeasonalBoostsAsync start: policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        var stmt = new GetPolicySeasonalBoostsStmt(SchemaName, policyKey, policyVersion);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<List<PolicySeasonalBoostDTO>>.Failure(exec);

        List<PolicySeasonalBoostDTO> copy = [.. stmt.Boosts];
        _logger.LogInformation("GetPolicySeasonalBoostsAsync complete: policyKey={PolicyKey}, version={Version}, count={Count}", policyKey, policyVersion, copy.Count);
        return Result<List<PolicySeasonalBoostDTO>>.Success(copy);
    }

    public async Task<Result<List<PolicySegmentOverrideDTO>>> GetPolicySegmentOverridesAsync(string policyKey, CancellationToken ct) {
        _logger.LogInformation("GetPolicySegmentOverridesAsync start: policyKey={PolicyKey}", policyKey);
        var stmt = new GetPolicySegmentOverridesStmt(SchemaName, policyKey);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);

        if (exec.IsFailure)
            return Result<List<PolicySegmentOverrideDTO>>.Failure(exec);

        List<PolicySegmentOverrideDTO> copy = [.. stmt.Overrides];
        _logger.LogInformation("GetPolicySegmentOverridesAsync complete: policyKey={PolicyKey}, count={Count}", policyKey, copy.Count);
        return Result<List<PolicySegmentOverrideDTO>>.Success(copy);
    }

    public async Task<Result<SeasonCalendarWithNextDTO>> GetCurrentSeasonAsync(CancellationToken ct) {
        _logger.LogInformation("GetCurrentSeasonAsync start.");
        var stmt = new GetCurrentSeasonStmt(SchemaName);
        Result exec = await Executor.ExecuteQueryWithRetry(stmt, ct);
        if (exec.IsFailure)
        {
            _logger.LogError("GetCurrentSeasonAsync failed: {Error}", exec.ErrorMessage);
            return Result<SeasonCalendarWithNextDTO>.Failure(exec);
        }

        SeasonCalendarWithNextDTO dto = stmt.Result;
        _logger.LogInformation("GetCurrentSeasonAsync succeeded: currentSeasonId={SeasonId}", dto.Current.SeasonId);
        return Result<SeasonCalendarWithNextDTO>.Success(dto);
    }

    #region PRIVATE HELPERS

    private async Task<List<PolicyStreakCurveEntryDTO>> NormalizeStreakEntriesAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicyStreakCurveEntryDTO> entries,
        CancellationToken ct) {

        List<PolicyStreakCurveEntryDTO> normalized = new(entries.Count);
        foreach (PolicyStreakCurveEntryDTO entry in entries) {
            long id = entry.StreakCurveId != 0 ? entry.StreakCurveId : (long)await GetNextId64(ct);
            normalized.Add(new PolicyStreakCurveEntryDTO(
                id,
                policyKey,
                policyVersion,
                entry.DayIndex,
                entry.Multiplier,
                entry.AdditiveBonusXp,
                entry.CapNextDay));
        }

        return normalized;
    }

    private async Task<List<PolicySeasonalBoostDTO>> NormalizeSeasonalBoostsAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        CancellationToken ct) {

        List<PolicySeasonalBoostDTO> normalized = new(boosts.Count);
        foreach (PolicySeasonalBoostDTO boost in boosts) {
            long id = boost.BoostId != 0 ? boost.BoostId : (long)await GetNextId64(ct);
            normalized.Add(new PolicySeasonalBoostDTO(
                id,
                policyKey,
                policyVersion,
                boost.Label,
                boost.Multiplier,
                boost.StartUtc,
                boost.EndUtc));
        }

        return normalized;
    }

    private async Task<List<PolicySegmentOverrideDTO>> NormalizeSegmentOverridesAsync(
        string policyKey,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides,
        CancellationToken ct) {

        List<PolicySegmentOverrideDTO> normalized = new(overrides.Count);
        foreach (PolicySegmentOverrideDTO dto in overrides) {
            long id = dto.OverrideId != 0 ? dto.OverrideId : (long)await GetNextId64(ct);
            normalized.Add(new PolicySegmentOverrideDTO(
                id,
                dto.SegmentKey,
                policyKey,
                dto.TargetPolicyVersion,
                dto.CreatedAt,
                dto.CreatedBy));
        }

        return normalized;
    }

    private async Task<Result> ReplaceStreakCurveAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicyStreakCurveEntryDTO> entries,
        TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx,
        CancellationToken ct) {

        var deleteStmt = new DeletePolicyStreakCurveStmt(SchemaName, policyKey, policyVersion);
        _logger.LogInformation("Executing DeletePolicyStreakCurveStmt for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        Result deleteResult = await Executor.ExecuteUnderTransactionWithRetry(deleteStmt, tx, ct);
        if (deleteResult.IsFailure) {
            _logger.LogError("DeletePolicyStreakCurveStmt failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, deleteResult.ErrorMessage);
            return deleteResult;
        }
        _logger.LogInformation("DeletePolicyStreakCurveStmt succeeded for policyKey={PolicyKey}, version={Version}, rowsAffected={Rows}", policyKey, policyVersion, deleteStmt.NumRowsAffected);

        foreach (PolicyStreakCurveEntryDTO entry in entries) {
            var insertStmt = new InsertPolicyStreakCurveEntryStmt(
                SchemaName,
                entry.StreakCurveId,
                policyKey,
                policyVersion,
                entry.DayIndex,
                entry.Multiplier,
                entry.AdditiveBonusXp,
                entry.CapNextDay);

            _logger.LogInformation("Executing InsertPolicyStreakCurveEntryStmt for policyKey={PolicyKey}, version={Version}, dayIndex={DayIndex}", policyKey, policyVersion, entry.DayIndex);
            Result insertResult = await Executor.ExecuteUnderTransactionWithRetry(insertStmt, tx, ct);
            if (insertResult.IsFailure) {
                _logger.LogError("InsertPolicyStreakCurveEntryStmt failed for policyKey={PolicyKey}, version={Version}, dayIndex={DayIndex}: {Error}", policyKey, policyVersion, entry.DayIndex, insertResult.ErrorMessage);
                return insertResult;
            }
            _logger.LogInformation("InsertPolicyStreakCurveEntryStmt succeeded for policyKey={PolicyKey}, version={Version}, dayIndex={DayIndex}, rowsAffected={Rows}", policyKey, policyVersion, entry.DayIndex, insertStmt.NumRowsAffected);
        }

        return Result.Success;
    }

    private async Task<Result> ReplaceSeasonalBoostsAsync(
        string policyKey,
        long policyVersion,
        IReadOnlyList<PolicySeasonalBoostDTO> boosts,
        TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx,
        CancellationToken ct) {

        var deleteStmt = new DeletePolicySeasonalBoostsStmt(SchemaName, policyKey, policyVersion);
        _logger.LogInformation("Executing DeletePolicySeasonalBoostsStmt for policyKey={PolicyKey}, version={Version}", policyKey, policyVersion);
        Result deleteResult = await Executor.ExecuteUnderTransactionWithRetry(deleteStmt, tx, ct);
        if (deleteResult.IsFailure) {
            _logger.LogError("DeletePolicySeasonalBoostsStmt failed for policyKey={PolicyKey}, version={Version}: {Error}", policyKey, policyVersion, deleteResult.ErrorMessage);
            return deleteResult;
        }
        _logger.LogInformation("DeletePolicySeasonalBoostsStmt succeeded for policyKey={PolicyKey}, version={Version}, rowsAffected={Rows}", policyKey, policyVersion, deleteStmt.NumRowsAffected);

        foreach (PolicySeasonalBoostDTO boost in boosts) {
            var insertStmt = new InsertPolicySeasonalBoostStmt(
                SchemaName,
                policyKey,
                policyVersion,
                boost.BoostId,
                boost.Label,
                boost.Multiplier,
                boost.StartUtc,
                boost.EndUtc);

            _logger.LogInformation("Executing InsertPolicySeasonalBoostStmt for policyKey={PolicyKey}, version={Version}, boostId={BoostId}", policyKey, policyVersion, boost.BoostId);
            Result insertResult = await Executor.ExecuteUnderTransactionWithRetry(insertStmt, tx, ct);
            if (insertResult.IsFailure) {
                _logger.LogError("InsertPolicySeasonalBoostStmt failed for policyKey={PolicyKey}, version={Version}, boostId={BoostId}: {Error}", policyKey, policyVersion, boost.BoostId, insertResult.ErrorMessage);
                return insertResult;
            }
            _logger.LogInformation("InsertPolicySeasonalBoostStmt succeeded for policyKey={PolicyKey}, version={Version}, boostId={BoostId}, rowsAffected={Rows}", policyKey, policyVersion, boost.BoostId, insertStmt.NumRowsAffected);
        }

        return Result.Success;
    }

    private async Task<Result> ReplaceSegmentOverridesAsync(
        string policyKey,
        IReadOnlyList<PolicySegmentOverrideDTO> overrides,
        TransactionBase<NpgsqlConnection, NpgsqlTransaction> tx,
        CancellationToken ct) {

        var deleteStmt = new DeletePolicySegmentOverridesStmt(SchemaName, policyKey);
        _logger.LogInformation("Executing DeletePolicySegmentOverridesStmt for policyKey={PolicyKey}", policyKey);
        Result deleteResult = await Executor.ExecuteUnderTransactionWithRetry(deleteStmt, tx, ct);
        if (deleteResult.IsFailure) {
            _logger.LogError("DeletePolicySegmentOverridesStmt failed for policyKey={PolicyKey}: {Error}", policyKey, deleteResult.ErrorMessage);
            return deleteResult;
        }
        _logger.LogInformation("DeletePolicySegmentOverridesStmt succeeded for policyKey={PolicyKey}, rowsAffected={Rows}", policyKey, deleteStmt.NumRowsAffected);

        foreach (PolicySegmentOverrideDTO dto in overrides) {
            var insertStmt = new InsertPolicySegmentOverrideStmt(
                SchemaName,
                dto.OverrideId,
                dto.SegmentKey,
                policyKey,
                dto.TargetPolicyVersion,
                dto.CreatedAt,
                dto.CreatedBy);

            _logger.LogInformation("Executing InsertPolicySegmentOverrideStmt for policyKey={PolicyKey}, segment={Segment}", policyKey, dto.SegmentKey);
            Result insertResult = await Executor.ExecuteUnderTransactionWithRetry(insertStmt, tx, ct);
            if (insertResult.IsFailure) {
                _logger.LogError("InsertPolicySegmentOverrideStmt failed for policyKey={PolicyKey}, segment={Segment}: {Error}", policyKey, dto.SegmentKey, insertResult.ErrorMessage);
                return insertResult;
            }
            _logger.LogInformation("InsertPolicySegmentOverrideStmt succeeded for policyKey={PolicyKey}, segment={Segment}, rowsAffected={Rows}", policyKey, dto.SegmentKey, insertStmt.NumRowsAffected);
        }

        return Result.Success;
    }

    #endregion PRIVATE HELPERS
}
