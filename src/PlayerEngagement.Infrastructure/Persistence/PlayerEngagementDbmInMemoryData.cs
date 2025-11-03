using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerEngagement.Infrastructure.Persistence;

internal class PlayerEngagementDbmInMemoryData {
    // Static shared storage that persists across all instances
    private static readonly object _lock = new object();
}
