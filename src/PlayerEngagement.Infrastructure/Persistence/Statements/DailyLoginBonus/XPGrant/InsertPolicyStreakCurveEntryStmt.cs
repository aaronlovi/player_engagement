using System.Collections.Generic;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class InsertPolicyStreakCurveEntryStmt : PostgresNonQueryDbStmtBase {
    private const string SqlTemplate = @"
insert into ${schema}.xp_policy_streak_curve (
    streak_curve_id,
    policy_key,
    policy_version,
    day_index,
    multiplier,
    additive_bonus_xp,
    cap_next_day)
values (
    @streak_curve_id,
    @policy_key,
    @policy_version,
    @day_index,
    @multiplier,
    @additive_bonus_xp,
    @cap_next_day);
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly long _streakCurveId;
    private readonly string _policyKey;
    private readonly long _policyVersion;
    private readonly int _dayIndex;
    private readonly decimal _multiplier;
    private readonly int _additiveBonusXp;
    private readonly bool _capNextDay;

    internal InsertPolicyStreakCurveEntryStmt(
        string schemaName,
        long streakCurveId,
        string policyKey,
        long policyVersion,
        int dayIndex,
        decimal multiplier,
        int additiveBonusXp,
        bool capNextDay)
        : base(GetSql(schemaName), nameof(InsertPolicyStreakCurveEntryStmt)) {
        _streakCurveId = streakCurveId;
        _policyKey = policyKey;
        _policyVersion = policyVersion;
        _dayIndex = dayIndex;
        _multiplier = multiplier;
        _additiveBonusXp = additiveBonusXp;
        _capNextDay = capNextDay;
    }

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [
            new NpgsqlParameter("streak_curve_id", _streakCurveId),
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_version", _policyVersion),
            new NpgsqlParameter("day_index", _dayIndex),
            new NpgsqlParameter("multiplier", _multiplier),
            new NpgsqlParameter("additive_bonus_xp", _additiveBonusXp),
            new NpgsqlParameter("cap_next_day", _capNextDay)
        ];

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
