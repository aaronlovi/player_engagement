using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Npgsql;
using PlayerEngagement.Infrastructure.Persistence.DTOs.XpPolicyDTOs;
using PlayerEngagement.Infrastructure.Persistence.Statements;
using PlayerEngagement.Infrastructure.Tests.TestUtilities;
using Xunit;

namespace PlayerEngagement.Infrastructure.Tests.Persistence.Statements;

public sealed class GetPolicyStreakCurveStmtTests {
    [Fact]
    public void GetBoundParameters_BindsKeyAndVersion() {
        var stmt = new GetPolicyStreakCurveStmt("pe", "daily-login", 7);

        IReadOnlyCollection<NpgsqlParameter> parameters = StatementTestHelper.GetParameters(stmt);

        Assert.Equal(2, parameters.Count);
        _ = Assert.Single(parameters, p => p.ParameterName == "policy_key" && (string)p.Value! == "daily-login");
        _ = Assert.Single(parameters, p => p.ParameterName == "policy_version" && (int)p.Value! == 7);
    }

    [Fact]
    public void ProcessCurrentRow_AppendsEntriesInOrder() {
        DataTable table = new DataTableBuilder()
            .WithColumn("streak_curve_id", typeof(long), 1L)
            .WithColumn("policy_key", typeof(string), "daily-login")
            .WithColumn("policy_version", typeof(int), 7)
            .WithColumn("day_index", typeof(int), 1)
            .WithColumn("multiplier", typeof(decimal), 1.5m)
            .WithColumn("additive_bonus_xp", typeof(int), 25)
            .WithColumn("cap_next_day", typeof(bool), false)
            .ToTable();

        using DbDataReader reader = table.CreateDataReader();
        _ = reader.Read();
        var stmt = new GetPolicyStreakCurveStmt("pe", "daily-login", 7);

        StatementTestHelper.InvokeBeforeRowProcessing(stmt, reader);
        bool continueProcessing = StatementTestHelper.InvokeProcessCurrentRow(stmt, reader);

        Assert.True(continueProcessing);
        _ = Assert.Single(stmt.Entries);
        PolicyStreakCurveEntryDTO entry = stmt.Entries[0];
        Assert.Equal(1.5m, entry.Multiplier);
        Assert.Equal(25, entry.AdditiveBonusXp);
    }
}
