using System;
using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class InsertPolicySegmentOverrideStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"
insert into ${schema}.xp_policy_segment_overrides (
    override_id,
    segment_key,
    policy_key,
    target_policy_version,
    created_at,
    created_by)
values (
    @override_id,
    @segment_key,
    @policy_key,
    @target_policy_version,
    @created_at,
    @created_by);
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly long _overrideId;
    private readonly string _segmentKey;
    private readonly string _policyKey;
    private readonly long _targetPolicyVersion;
    private readonly DateTime _createdAt;
    private readonly string _createdBy;

    internal InsertPolicySegmentOverrideStmt(
        string schemaName,
        long overrideId,
        string segmentKey,
        string policyKey,
        long targetPolicyVersion,
        DateTime createdAt,
        string createdBy)
        : base(GetSql(schemaName), nameof(InsertPolicySegmentOverrideStmt)) {
        _overrideId = overrideId;
        _segmentKey = segmentKey;
        _policyKey = policyKey;
        _targetPolicyVersion = targetPolicyVersion;
        _createdAt = createdAt;
        _createdBy = createdBy;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [
            new NpgsqlParameter("override_id", _overrideId),
            new NpgsqlParameter("segment_key", _segmentKey),
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("target_policy_version", _targetPolicyVersion),
            new NpgsqlParameter("created_at", _createdAt),
            new NpgsqlParameter("created_by", _createdBy)
        ];

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
