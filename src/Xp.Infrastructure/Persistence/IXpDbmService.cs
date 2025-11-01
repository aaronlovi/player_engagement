using System.Threading;
using System.Threading.Tasks;
using InnoAndLogic.Shared;

namespace Xp.Infrastructure.Persistence;

public interface IXpDbmService
{
    Task<Result> HealthCheckAsync(CancellationToken ct);
}
