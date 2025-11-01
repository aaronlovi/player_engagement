using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Persistence;
using InnoAndLogic.Persistence.Migrations;
using InnoAndLogic.Shared;
using Microsoft.Extensions.Logging;

namespace Xp.Infrastructure.Persistence;

public sealed class XpDbmService : DbmService, IXpDbmService
{
    private readonly ILogger<XpDbmService> _logger;

    public XpDbmService(
        ILoggerFactory loggerFactory,
        PostgresExecutor executor,
        DatabaseOptions options,
        DbMigrations migrations) : base(loggerFactory, executor, options, migrations)
    {
        _logger = loggerFactory.CreateLogger<XpDbmService>();
    }

    public Task<Result> HealthCheckAsync(CancellationToken ct)
    {
        _logger.LogInformation("XP DBM health check requested.");
        return Task.FromResult(Result.Success);
    }
}
