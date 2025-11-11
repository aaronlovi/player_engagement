using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Npgsql;
using PlayerEngagement.Infrastructure.Persistence.Statements;
using PlayerEngagement.Infrastructure.Tests.TestUtilities;
using Xunit;

namespace PlayerEngagement.Infrastructure.Tests.Persistence.Statements;

public sealed class GetActivePolicyStmtTests {
    [Fact]
    public void GetBoundParameters_BindsPolicyKeyAndTimestamp() {
        DateTime utcNow = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var stmt = new GetActivePolicyStmt("pe", "daily-login", utcNow);

        IReadOnlyCollection<NpgsqlParameter> parameters = StatementTestHelper.GetParameters(stmt);

        Assert.Equal(2, parameters.Count);
        _ = Assert.Single(parameters, p => p.ParameterName == "policy_key" && (string)p.Value! == "daily-login");
        _ = Assert.Single(parameters, p => p.ParameterName == "now_utc" && (DateTime)p.Value! == utcNow);
    }

    [Fact]
    public void ProcessCurrentRow_PopulatesActivePolicyDto() {
        DateTime now = new(2024, 2, 4, 18, 30, 0, DateTimeKind.Utc);
        DataTable table = new DataTableBuilder()
            .WithColumn("policy_id", typeof(long), 10L)
            .WithColumn("policy_key", typeof(string), "daily-login")
            .WithColumn("display_name", typeof(string), "Daily Login")
            .WithColumn("description", typeof(string), "desc")
            .WithColumn("policy_version", typeof(int), 3)
            .WithColumn("status", typeof(string), "Published")
            .WithColumn("base_xp_amount", typeof(int), 50)
            .WithColumn("currency", typeof(string), "XP")
            .WithColumn("claim_window_start_minutes", typeof(int), 60)
            .WithColumn("claim_window_duration_hours", typeof(int), 6)
            .WithColumn("anchor_strategy", typeof(string), "ANCHOR_TIMEZONE")
            .WithColumn("grace_allowed_misses", typeof(int), 1)
            .WithColumn("grace_window_days", typeof(int), 3)
            .WithColumn("streak_model_type", typeof(string), "PLATEAU_CAP")
            .WithColumn("streak_model_parameters", typeof(string), "{\"cap\":5}")
            .WithColumn("preview_sample_window_days", typeof(int), 7)
            .WithColumn("preview_default_segment", typeof(string), "default")
            .WithColumn("seasonal_metadata", typeof(string), "{\"event\":\"spring\"}")
            .WithColumn("effective_at", typeof(DateTime), now)
            .WithColumn("superseded_at", typeof(DateTime), DBNull.Value)
            .WithColumn("created_at", typeof(DateTime), now.AddDays(-2))
            .WithColumn("created_by", typeof(string), "agent")
            .WithColumn("published_at", typeof(DateTime), now.AddDays(-1))
            .ToTable();

        using DbDataReader reader = table.CreateDataReader();
        _ = reader.Read();
        var stmt = new GetActivePolicyStmt("pe", "daily-login", now);

        StatementTestHelper.InvokeBeforeRowProcessing(stmt, reader);
        bool continueProcessing = StatementTestHelper.InvokeProcessCurrentRow(stmt, reader);

        Assert.False(continueProcessing);
        Assert.Equal("daily-login", stmt.ActivePolicy.PolicyKey);
        Assert.Equal(3, stmt.ActivePolicy.PolicyVersion);
        Assert.Equal("Daily Login", stmt.ActivePolicy.DisplayName);
    }
}
