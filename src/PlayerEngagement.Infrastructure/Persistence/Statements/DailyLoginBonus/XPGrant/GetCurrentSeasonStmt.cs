using System.Collections.Generic;
using System.Data.Common;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;
using PlayerEngagement.Infrastructure.Persistence.DTOs.Seasons;

namespace PlayerEngagement.Infrastructure.Persistence.Statements.DailyLoginBonus.XPGrant;

internal sealed class GetCurrentSeasonStmt : PostgresQueryDbStmtBase
{
    private const string SqlTemplate = @"
with active as (
    select season_id, label, start_date, end_date
    from ${schema}.daily_login_bonus_xp_seasons
    where start_date <= current_date and end_date >= current_date
    order by start_date asc
    limit 1
),
next as (
    select season_id, label, start_date, end_date
    from ${schema}.daily_login_bonus_xp_seasons
    where start_date > current_date
    order by start_date asc
    limit 1
)
select
    coalesce(a.season_id, 0) as current_season_id,
    coalesce(a.label, '') as current_label,
    coalesce(a.start_date, '0001-01-01') as current_start_date,
    coalesce(a.end_date, '0001-01-01') as current_end_date,
    coalesce(n.season_id, 0) as next_season_id,
    coalesce(n.label, '') as next_label,
    coalesce(n.start_date, '0001-01-01') as next_start_date,
    coalesce(n.end_date, '0001-01-01') as next_end_date
from active a
cross join next n;
";

    private static string? _sql;
    private static readonly object SqlLock = new();

    private static int _currentIdIndex = -1;
    private static int _currentLabelIndex = -1;
    private static int _currentStartIndex = -1;
    private static int _currentEndIndex = -1;
    private static int _nextIdIndex = -1;
    private static int _nextLabelIndex = -1;
    private static int _nextStartIndex = -1;
    private static int _nextEndIndex = -1;

    private readonly List<SeasonCalendarWithNextDTO> _results = new();

    internal GetCurrentSeasonStmt(string schemaName)
        : base(GetSql(schemaName), nameof(GetCurrentSeasonStmt))
    {
    }

    internal SeasonCalendarWithNextDTO Result => _results.Count > 0 ? _results[0] : SeasonCalendarWithNextDTO.Empty;

    protected override void ClearResults() => _results.Clear();

    protected override IReadOnlyCollection<NpgsqlParameter> GetBoundParameters() => [];

    protected override void BeforeRowProcessing(DbDataReader reader)
    {
        base.BeforeRowProcessing(reader);
        if (_currentIdIndex != -1)
            return;

        _currentIdIndex = reader.GetOrdinal("current_season_id");
        _currentLabelIndex = reader.GetOrdinal("current_label");
        _currentStartIndex = reader.GetOrdinal("current_start_date");
        _currentEndIndex = reader.GetOrdinal("current_end_date");
        _nextIdIndex = reader.GetOrdinal("next_season_id");
        _nextLabelIndex = reader.GetOrdinal("next_label");
        _nextStartIndex = reader.GetOrdinal("next_start_date");
        _nextEndIndex = reader.GetOrdinal("next_end_date");
    }

    protected override bool ProcessCurrentRow(DbDataReader reader)
    {
        SeasonCalendarDTO current = new(
            reader.GetInt64(_currentIdIndex),
            reader.GetString(_currentLabelIndex),
            reader.GetDateTime(_currentStartIndex),
            reader.GetDateTime(_currentEndIndex));

        SeasonCalendarDTO next = new(
            reader.GetInt64(_nextIdIndex),
            reader.GetString(_nextLabelIndex),
            reader.GetDateTime(_nextStartIndex),
            reader.GetDateTime(_nextEndIndex));

        _results.Add(new SeasonCalendarWithNextDTO(current, next));
        return false; // Only one row expected
    }

    private static string GetSql(string schemaName)
    {
        if (_sql is null)
        {
            lock (SqlLock)
                _sql ??= SqlTemplate.Replace("${schema}", schemaName);
        }

        return _sql;
    }
}
