using System.Collections.Generic;
using System.Data.Common;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence.Statements;

internal sealed class GetPolicyStreakCurveStmt : PostgresQueryDbStmtBase {
    private const string SqlTemplate = @"
select streak_curve_id,
       policy_key,
       policy_version,
       day_index,
       multiplier,
       additive_bonus_xp,
       cap_next_day
from ${schema}.xp_policy_streak_curve
where policy_key = @policy_key
  and policy_version = @policy_version
order by day_index asc;
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private static int _idIndex = -1;
    private static int _policyKeyIndex = -1;
    private static int _policyVersionIndex = -1;
    private static int _dayIndex = -1;
    private static int _multiplierIndex = -1;
    private static int _bonusIndex = -1;
    private static int _capIndex = -1;

    private readonly string _policyKey;
    private readonly long _policyVersion;

    internal List<PolicyStreakCurveEntryDTO> Entries { get; } = new();

    internal GetPolicyStreakCurveStmt(string schemaName, string policyKey, long policyVersion)
        : base(GetSql(schemaName), nameof(GetPolicyStreakCurveStmt)) {
        _policyKey = policyKey;
        _policyVersion = policyVersion;
    }

    protected override void ClearResults() => Entries.Clear();

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        new[] {
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_version", _policyVersion)
        };

    protected override void BeforeRowProcessing(DbDataReader reader) {
        base.BeforeRowProcessing(reader);

        if (_idIndex != -1)
            return;

        _idIndex = reader.GetOrdinal("streak_curve_id");
        _policyKeyIndex = reader.GetOrdinal("policy_key");
        _policyVersionIndex = reader.GetOrdinal("policy_version");
        _dayIndex = reader.GetOrdinal("day_index");
        _multiplierIndex = reader.GetOrdinal("multiplier");
        _bonusIndex = reader.GetOrdinal("additive_bonus_xp");
        _capIndex = reader.GetOrdinal("cap_next_day");
    }

    protected override bool ProcessCurrentRow(DbDataReader reader) {
        PolicyStreakCurveEntryDTO dto = new(
            reader.GetInt64(_idIndex),
            reader.GetString(_policyKeyIndex),
            reader.GetInt64(_policyVersionIndex),
            reader.GetInt32(_dayIndex),
            reader.GetDecimal(_multiplierIndex),
            reader.GetInt32(_bonusIndex),
            reader.GetBoolean(_capIndex));

        Entries.Add(dto);
        return true;
    }

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
