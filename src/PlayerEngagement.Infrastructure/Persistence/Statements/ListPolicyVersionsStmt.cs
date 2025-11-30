using System;
using System.Collections.Generic;
using System.Data.Common;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;

namespace PlayerEngagement.Infrastructure.Persistence.Statements;

internal sealed class ListPolicyVersionsStmt : PostgresQueryDbStmtBase {
    private const string SqlTemplate = @"
select p.policy_id,
       p.policy_key,
       p.display_name,
       coalesce(p.description, '') as description,
       v.policy_version,
       v.status,
       v.base_xp_amount,
       v.currency,
       v.claim_window_start_minutes,
       v.claim_window_duration_hours,
       v.anchor_strategy,
       v.grace_allowed_misses,
       v.grace_window_days,
       v.streak_model_type,
       v.streak_model_parameters::text as streak_model_parameters,
       v.preview_sample_window_days,
       coalesce(v.preview_default_segment, '') as preview_default_segment,
       v.seasonal_metadata::text as seasonal_metadata,
       v.effective_at,
       v.superseded_at,
       v.created_at,
       v.created_by,
       v.published_at
from ${schema}.xp_policies p
join ${schema}.xp_policy_versions v on p.policy_key = v.policy_key
where p.policy_key = @policy_key
  and (@status is null or v.status = @status)
  and (@effective_before is null or v.effective_at is null or v.effective_at <= @effective_before)
order by v.policy_version desc
limit @limit_rows;
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;
    private readonly string? _status;
    private readonly DateTime? _effectiveBefore;
    private readonly int _limit;

    internal List<PolicyVersionDTO> Versions { get; } = new();

    internal ListPolicyVersionsStmt(string schemaName, string policyKey, string? status, DateTime? effectiveBefore, int limit)
        : base(GetSql(schemaName), nameof(ListPolicyVersionsStmt)) {
        _policyKey = policyKey;
        _status = string.IsNullOrWhiteSpace(status) ? null : status;
        _effectiveBefore = effectiveBefore;
        _limit = limit;
    }

    protected override void ClearResults() => Versions.Clear();

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        new[] {
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("status", _status ?? (object)DBNull.Value),
            new NpgsqlParameter("effective_before", _effectiveBefore ?? (object)DBNull.Value),
            new NpgsqlParameter("limit_rows", _limit)
        };

    protected override void BeforeRowProcessing(DbDataReader reader) {
        base.BeforeRowProcessing(reader);
        PolicyVersionRowMapper.EnsureOrdinals(reader);
    }

    protected override bool ProcessCurrentRow(DbDataReader reader) {
        Versions.Add(PolicyVersionRowMapper.ReadPolicyVersion(reader));
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
