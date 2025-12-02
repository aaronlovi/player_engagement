using System;
using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class RetirePolicyVersionStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"
update ${schema}.xp_policy_versions
set status = 'Archived',
    superseded_at = @superseded_at
where policy_key = @policy_key
  and policy_version = @policy_version
  and status = 'Published';
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;
    private readonly long _policyVersion;
    private readonly DateTime _supersededAt;

    internal RetirePolicyVersionStmt(string schemaName, string policyKey, long policyVersion, DateTime supersededAt)
        : base(GetSql(schemaName), nameof(RetirePolicyVersionStmt)) {
        _policyKey = policyKey;
        _policyVersion = policyVersion;
        _supersededAt = supersededAt;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_version", _policyVersion),
            new NpgsqlParameter("superseded_at", _supersededAt)
        ];

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
