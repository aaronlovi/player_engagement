using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;

namespace PlayerEngagement.Infrastructure.Persistence;

public interface IPlayerEngagementDbmService
{
    Task<Result> HealthCheckAsync(CancellationToken ct);
}
