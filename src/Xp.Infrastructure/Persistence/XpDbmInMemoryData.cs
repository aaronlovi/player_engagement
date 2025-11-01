using System;
using System.Collections.Generic;
using System.Linq;

namespace Xp.Infrastructure.Persistence;

internal class XpDbmInMemoryData {
    // Static shared storage that persists across all instances
    private static readonly object _lock = new object();
}
