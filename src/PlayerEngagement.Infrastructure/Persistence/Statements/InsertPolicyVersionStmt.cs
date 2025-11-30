using System;
using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements;

internal sealed class InsertPolicyVersionStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"
insert into ${schema}.xp_policy_versions (
    policy_key,
    policy_version,
    status,
    base_xp_amount,
    currency,
    claim_window_start_minutes,
    claim_window_duration_hours,
    anchor_strategy,
    grace_allowed_misses,
    grace_window_days,
    streak_model_type,
    streak_model_parameters,
    preview_sample_window_days,
    preview_default_segment,
    seasonal_metadata,
    effective_at,
    superseded_at,
    created_at,
    created_by,
    published_at)
values (
    @policy_key,
    @policy_version,
    @status,
    @base_xp_amount,
    @currency,
    @claim_window_start_minutes,
    @claim_window_duration_hours,
    @anchor_strategy,
    @grace_allowed_misses,
    @grace_window_days,
    @streak_model_type,
    @streak_model_parameters::jsonb,
    @preview_sample_window_days,
    @preview_default_segment,
    @seasonal_metadata::jsonb,
    @effective_at,
    @superseded_at,
    @created_at,
    @created_by,
    @published_at);
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;
    private readonly long _policyVersion;
    private readonly string _status;
    private readonly int _baseXpAmount;
    private readonly string _currency;
    private readonly int _claimWindowStartMinutes;
    private readonly int _claimWindowDurationHours;
    private readonly string _anchorStrategy;
    private readonly int _graceAllowedMisses;
    private readonly int _graceWindowDays;
    private readonly string _streakModelType;
    private readonly string _streakModelParameters;
    private readonly int _previewSampleWindowDays;
    private readonly string? _previewDefaultSegment;
    private readonly string _seasonalMetadata;
    private readonly DateTime? _effectiveAt;
    private readonly DateTime? _supersededAt;
    private readonly DateTime _createdAt;
    private readonly string _createdBy;
    private readonly DateTime? _publishedAt;

    internal InsertPolicyVersionStmt(
        string schemaName,
        string policyKey,
        long policyVersion,
        string status,
        int baseXpAmount,
        string currency,
        int claimWindowStartMinutes,
        int claimWindowDurationHours,
        string anchorStrategy,
        int graceAllowedMisses,
        int graceWindowDays,
        string streakModelType,
        string streakModelParameters,
        int previewSampleWindowDays,
        string? previewDefaultSegment,
        string seasonalMetadata,
        DateTime? effectiveAt,
        DateTime? supersededAt,
        DateTime createdAt,
        string createdBy,
        DateTime? publishedAt)
        : base(GetSql(schemaName), nameof(InsertPolicyVersionStmt)) {
        _policyKey = policyKey;
        _policyVersion = policyVersion;
        _status = status;
        _baseXpAmount = baseXpAmount;
        _currency = currency;
        _claimWindowStartMinutes = claimWindowStartMinutes;
        _claimWindowDurationHours = claimWindowDurationHours;
        _anchorStrategy = anchorStrategy;
        _graceAllowedMisses = graceAllowedMisses;
        _graceWindowDays = graceWindowDays;
        _streakModelType = streakModelType;
        _streakModelParameters = streakModelParameters;
        _previewSampleWindowDays = previewSampleWindowDays;
        _previewDefaultSegment = previewDefaultSegment;
        _seasonalMetadata = seasonalMetadata;
        _effectiveAt = effectiveAt;
        _supersededAt = supersededAt;
        _createdAt = createdAt;
        _createdBy = createdBy;
        _publishedAt = publishedAt;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        new[] {
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_version", _policyVersion),
            new NpgsqlParameter("status", _status),
            new NpgsqlParameter("base_xp_amount", _baseXpAmount),
            new NpgsqlParameter("currency", _currency),
            new NpgsqlParameter("claim_window_start_minutes", _claimWindowStartMinutes),
            new NpgsqlParameter("claim_window_duration_hours", _claimWindowDurationHours),
            new NpgsqlParameter("anchor_strategy", _anchorStrategy),
            new NpgsqlParameter("grace_allowed_misses", _graceAllowedMisses),
            new NpgsqlParameter("grace_window_days", _graceWindowDays),
            new NpgsqlParameter("streak_model_type", _streakModelType),
            new NpgsqlParameter("streak_model_parameters", _streakModelParameters),
            new NpgsqlParameter("preview_sample_window_days", _previewSampleWindowDays),
            new NpgsqlParameter("preview_default_segment", _previewDefaultSegment ?? (object)DBNull.Value),
            new NpgsqlParameter("seasonal_metadata", _seasonalMetadata),
            new NpgsqlParameter("effective_at", _effectiveAt ?? (object)DBNull.Value),
            new NpgsqlParameter("superseded_at", _supersededAt ?? (object)DBNull.Value),
            new NpgsqlParameter("created_at", _createdAt),
            new NpgsqlParameter("created_by", _createdBy),
            new NpgsqlParameter("published_at", _publishedAt ?? (object)DBNull.Value)
        };

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
