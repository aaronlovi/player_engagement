using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class DeletePolicyStreakCurveStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"delete from ${schema}.daily_login_bonus_xp_policy_streak_curve where policy_key = @policy_key and policy_version = @policy_version;";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;
    private readonly long _policyVersion;

    internal DeletePolicyStreakCurveStmt(string schemaName, string policyKey, long policyVersion)
        : base(GetSql(schemaName), nameof(DeletePolicyStreakCurveStmt)) {
        _policyKey = policyKey;
        _policyVersion = policyVersion;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_version", _policyVersion)
        ];

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
