using System;
using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class InsertPolicySeasonalBoostStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"
insert into ${schema}.xp_policy_seasonal_boosts (
    policy_key,
    policy_version,
    boost_id,
    label,
    multiplier,
    start_utc,
    end_utc)
values (
    @policy_key,
    @policy_version,
    @boost_id,
    @label,
    @multiplier,
    @start_utc,
    @end_utc);
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;
    private readonly long _policyVersion;
    private readonly long _boostId;
    private readonly string _label;
    private readonly decimal _multiplier;
    private readonly DateTime _startUtc;
    private readonly DateTime _endUtc;

    internal InsertPolicySeasonalBoostStmt(
        string schemaName,
        string policyKey,
        long policyVersion,
        long boostId,
        string label,
        decimal multiplier,
        DateTime startUtc,
        DateTime endUtc)
        : base(GetSql(schemaName), nameof(InsertPolicySeasonalBoostStmt)) {
        _policyKey = policyKey;
        _policyVersion = policyVersion;
        _boostId = boostId;
        _label = label;
        _multiplier = multiplier;
        _startUtc = startUtc;
        _endUtc = endUtc;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_version", _policyVersion),
            new NpgsqlParameter("boost_id", _boostId),
            new NpgsqlParameter("label", _label),
            new NpgsqlParameter("multiplier", _multiplier),
            new NpgsqlParameter("start_utc", _startUtc),
            new NpgsqlParameter("end_utc", _endUtc)
        ];

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
