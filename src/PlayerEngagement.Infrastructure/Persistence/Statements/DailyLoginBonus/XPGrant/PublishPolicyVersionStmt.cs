using System;
using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class PublishPolicyVersionStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"
update ${schema}.xp_policy_versions
set status = 'Published',
    effective_at = @effective_at,
    superseded_at = null,
    published_at = @published_at
where policy_key = @policy_key
  and policy_version = @policy_version
  and status in ('Draft', 'Archived');
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;
    private readonly long _policyVersion;
    private readonly DateTime? _effectiveAt;
    private readonly DateTime _publishedAt;

    internal PublishPolicyVersionStmt(string schemaName, string policyKey, long policyVersion, DateTime? effectiveAt, DateTime publishedAt)
        : base(GetSql(schemaName), nameof(PublishPolicyVersionStmt)) {
        _policyKey = policyKey;
        _policyVersion = policyVersion;
        _effectiveAt = effectiveAt;
        _publishedAt = publishedAt;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_version", _policyVersion),
            new NpgsqlParameter("effective_at", _effectiveAt ?? (object)DBNull.Value),
            new NpgsqlParameter("published_at", _publishedAt)
        ];

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
