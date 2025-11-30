using System;
using System.Collections.Generic;
using System.Data.Common;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence.Statements;

internal sealed class GetPolicySeasonalBoostsStmt : PostgresQueryDbStmtBase {
    private const string SqlTemplate = @"""
select boost_id,
       policy_key,
       policy_version,
       label,
       multiplier,
       start_utc,
       end_utc
from ${schema}.xp_policy_seasonal_boosts
where policy_key = @policy_key
  and policy_version = @policy_version
order by start_utc asc;
""";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private static int _boostIdIndex = -1;
    private static int _policyKeyIndex = -1;
    private static int _policyVersionIndex = -1;
    private static int _labelIndex = -1;
    private static int _multiplierIndex = -1;
    private static int _startIndex = -1;
    private static int _endIndex = -1;

    private readonly string _policyKey;
    private readonly long _policyVersion;

    internal List<PolicySeasonalBoostDTO> Boosts { get; } = new();

    internal GetPolicySeasonalBoostsStmt(string schemaName, string policyKey, long policyVersion)
        : base(GetSql(schemaName), nameof(GetPolicySeasonalBoostsStmt)) {
        _policyKey = policyKey;
        _policyVersion = policyVersion;
    }

    protected override void ClearResults() => Boosts.Clear();

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        new[] {
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_version", _policyVersion)
        };

    protected override void BeforeRowProcessing(DbDataReader reader) {
        base.BeforeRowProcessing(reader);

        if (_boostIdIndex != -1)
            return;

        _boostIdIndex = reader.GetOrdinal("boost_id");
        _policyKeyIndex = reader.GetOrdinal("policy_key");
        _policyVersionIndex = reader.GetOrdinal("policy_version");
        _labelIndex = reader.GetOrdinal("label");
        _multiplierIndex = reader.GetOrdinal("multiplier");
        _startIndex = reader.GetOrdinal("start_utc");
        _endIndex = reader.GetOrdinal("end_utc");
    }

    protected override bool ProcessCurrentRow(DbDataReader reader) {
        PolicySeasonalBoostDTO dto = new(
            reader.GetInt64(_boostIdIndex),
            reader.GetString(_policyKeyIndex),
            reader.GetInt64(_policyVersionIndex),
            reader.GetString(_labelIndex),
            reader.GetDecimal(_multiplierIndex),
            reader.GetDateTime(_startIndex),
            reader.GetDateTime(_endIndex));

        Boosts.Add(dto);
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
