using System;
using System.Collections.Generic;
using System.Data.Common;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class EnsurePolicyShellStmt : PostgresQueryDbStmtBase {
    private const string SqlTemplate = @"
insert into ${schema}.xp_policies (policy_key, policy_id, display_name, description, created_at, created_by)
values (@policy_key, @policy_id, @display_name, @description, @created_at, @created_by)
on conflict (policy_key) do update
    set display_name = excluded.display_name,
        description = excluded.description
returning policy_id;
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private readonly string _policyKey;
    private readonly long _policyId;
    private readonly string _displayName;
    private readonly string? _description;
    private readonly DateTime _createdAt;
    private readonly string _createdBy;

    internal long PolicyId { get; private set; }

    internal EnsurePolicyShellStmt(
        string schemaName,
        string policyKey,
        long policyId,
        string displayName,
        string? description,
        DateTime createdAt,
        string createdBy)
        : base(GetSql(schemaName), nameof(EnsurePolicyShellStmt)) {
        _policyKey = policyKey;
        _policyId = policyId;
        _displayName = displayName;
        _description = description;
        _createdAt = createdAt;
        _createdBy = createdBy;
    }

    protected override void ClearResults() => PolicyId = 0;

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() =>
        [
            new NpgsqlParameter("policy_key", _policyKey),
            new NpgsqlParameter("policy_id", _policyId),
            new NpgsqlParameter("display_name", _displayName),
            new NpgsqlParameter("description", _description ?? string.Empty),
            new NpgsqlParameter("created_at", _createdAt),
            new NpgsqlParameter("created_by", _createdBy)
        ];

    protected override bool ProcessCurrentRow(DbDataReader reader) {
        PolicyId = reader.GetInt64(reader.GetOrdinal("policy_id"));
        return false;
    }

    private static string GetSql(string schemaName) {
        if (_sql is null) {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
