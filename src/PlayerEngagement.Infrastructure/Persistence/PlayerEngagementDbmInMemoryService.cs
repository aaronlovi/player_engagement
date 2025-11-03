using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging;

namespace PlayerEngagement.Infrastructure.Persistence;

public sealed class PlayerEngagementDbmInMemoryService : DbmInMemoryService, IPlayerEngagementDbmService, IDbmService
{
    private readonly ILogger<PlayerEngagementDbmInMemoryService> _logger;

    public PlayerEngagementDbmInMemoryService(ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PlayerEngagementDbmInMemoryService>();
        _logger.LogWarning("PlayerEngagementDbmInMemoryService instantiated: persistence in RAM only");
    }

    public Task<Result> HealthCheckAsync(CancellationToken ct)
    {
        _logger.LogInformation("Player Engagement in-memory DBM health check requested.");
        return Task.FromResult(Result.Success);
    }
}
