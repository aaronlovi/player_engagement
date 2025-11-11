using System;
using System.Collections.Generic;
using System.Data;

namespace PlayerEngagement.Infrastructure.Tests.TestUtilities;

internal sealed class DataTableBuilder {
    private readonly DataTable _table = new();
    private readonly List<object?> _values = [];

    internal DataTableBuilder WithColumn(string name, Type type, object? value) {
        _ = _table.Columns.Add(name, type);
        _values.Add(value);
        return this;
    }

    internal DataTable ToTable() {
        _ = _table.Rows.Add(_values.ToArray());
        return _table;
    }
}
