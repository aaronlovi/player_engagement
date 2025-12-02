using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class DeletePolicySegmentOverridesStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"delete from ${schema}.xp_policy_segment_overrides where policy_key = @policy_key;";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;

    internal DeletePolicySegmentOverridesStmt(string schemaName, string policyKey)
        : base(GetSql(schemaName), nameof(DeletePolicySegmentOverridesStmt)) {
        _policyKey = policyKey;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [new NpgsqlParameter("policy_key", _policyKey)];

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
