using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using InnoAndLogic.Persistence.Statements.Postgres;
using Npgsql;

namespace PlayerEngagement.Infrastructure.Tests.TestUtilities;

internal static class StatementTestHelper {
    internal static IReadOnlyCollection<NpgsqlParameter> GetParameters(PostgresQueryDbStmtBase statement) {
        MethodInfo method = statement.GetType().GetMethod("GetBoundParameters", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetBoundParameters reflection failed.");
        return (IReadOnlyCollection<NpgsqlParameter>)method.Invoke(statement, null)!;
    }

    internal static void InvokeBeforeRowProcessing(PostgresQueryDbStmtBase statement, DbDataReader reader) {
        MethodInfo method = statement.GetType().GetMethod("BeforeRowProcessing", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BeforeRowProcessing reflection failed.");
        _ = method.Invoke(statement, [reader]);
    }

    internal static bool InvokeProcessCurrentRow(PostgresQueryDbStmtBase statement, DbDataReader reader) {
        MethodInfo method = statement.GetType().GetMethod("ProcessCurrentRow", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ProcessCurrentRow reflection failed.");
        return (bool)method.Invoke(statement, [reader])!;
    }
}
