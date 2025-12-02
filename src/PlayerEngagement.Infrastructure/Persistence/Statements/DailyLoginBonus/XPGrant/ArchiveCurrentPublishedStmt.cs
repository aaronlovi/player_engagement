using System;
using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class ArchiveCurrentPublishedStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"
update ${schema}.xp_policy_versions
set status = 'Archived',
    superseded_at = @superseded_at
where policy_key = @policy_key
  and status = 'Published';
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;
    private readonly DateTime? _supersededAt;

    internal ArchiveCurrentPublishedStmt(string schemaName, string policyKey, DateTime? supersededAt)
        : base(GetSql(schemaName), nameof(ArchiveCurrentPublishedStmt)) {
        _policyKey = policyKey;
        _supersededAt = supersededAt;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("superseded_at", _supersededAt ?? (object)DBNull.Value)
        ];

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
