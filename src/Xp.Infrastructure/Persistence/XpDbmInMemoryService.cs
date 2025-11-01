using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging;

namespace Xp.Infrastructure.Persistence;

public sealed class XpDbmInMemoryService : DbmInMemoryService, IXpDbmService, IDbmService
{
    private readonly ILogger<XpDbmInMemoryService> _logger;

    public XpDbmInMemoryService(ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<XpDbmInMemoryService>();
        _logger.LogWarning("XpDbmInMemoryService instantiated: persistence in RAM only");
    }

    public Task<Result> HealthCheckAsync(CancellationToken ct)
    {
        _logger.LogInformation("XP in-memory DBM health check requested.");
        return Task.FromResult(Result.Success);
    }
}
