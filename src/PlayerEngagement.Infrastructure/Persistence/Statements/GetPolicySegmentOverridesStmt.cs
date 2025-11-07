using System.Collections.Generic;
using System.Data.Common;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence.Statements;

internal sealed class GetPolicySegmentOverridesStmt : PostgresQueryDbStmtBase {
    private const string SqlTemplate = @"""
select override_id,
       segment_key,
       policy_key,
       target_policy_version,
       created_at,
       created_by
from ${schema}.xp_policy_segment_overrides
where policy_key = @policy_key;
""";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private static int _overrideIdIndex = -1;
    private static int _segmentKeyIndex = -1;
    private static int _policyKeyIndex = -1;
    private static int _targetVersionIndex = -1;
    private static int _createdAtIndex = -1;
    private static int _createdByIndex = -1;

    private readonly string _policyKey;

    internal List<PolicySegmentOverrideDTO> Overrides { get; } = new();

    internal GetPolicySegmentOverridesStmt(string schemaName, string policyKey)
        : base(GetSql(schemaName), nameof(GetPolicySegmentOverridesStmt)) => _policyKey = policyKey;

    protected override void ClearResults() => Overrides.Clear();

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        new[] { new NpgsqlParameter("policy_key", _policyKey) };

    protected override void BeforeRowProcessing(DbDataReader reader) {
        base.BeforeRowProcessing(reader);

        if (_overrideIdIndex != -1)
            return;

        _overrideIdIndex = reader.GetOrdinal("override_id");
        _segmentKeyIndex = reader.GetOrdinal("segment_key");
        _policyKeyIndex = reader.GetOrdinal("policy_key");
        _targetVersionIndex = reader.GetOrdinal("target_policy_version");
        _createdAtIndex = reader.GetOrdinal("created_at");
        _createdByIndex = reader.GetOrdinal("created_by");
    }

    protected override bool ProcessCurrentRow(DbDataReader reader) {
        PolicySegmentOverrideDTO dto = new(
            reader.GetInt64(_overrideIdIndex),
            reader.GetString(_segmentKeyIndex),
            reader.GetString(_policyKeyIndex),
            reader.GetInt32(_targetVersionIndex),
            reader.GetDateTime(_createdAtIndex),
            reader.GetString(_createdByIndex));

        Overrides.Add(dto);
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
