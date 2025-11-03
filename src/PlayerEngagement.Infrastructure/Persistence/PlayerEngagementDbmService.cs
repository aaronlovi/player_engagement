using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Persistence.Migrations;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging;

namespace PlayerEngagement.Infrastructure.Persistence;

public sealed class PlayerEngagementDbmService : DbmService, IPlayerEngagementDbmService
{
    private readonly ILogger<PlayerEngagementDbmService> _logger;

    public PlayerEngagementDbmService(
        ILoggerFactory loggerFactory,
        PostgresExecutor executor,
        DatabaseOptions options,
        DbMigrations migrations) : base(loggerFactory, executor, options, migrations)
    {
        _logger = loggerFactory.CreateLogger<PlayerEngagementDbmService>();
    }

    public Task<Result> HealthCheckAsync(CancellationToken ct)
    {
        _logger.LogInformation("Player Engagement DBM health check requested.");
        return Task.FromResult(Result.Success);
    }
}
