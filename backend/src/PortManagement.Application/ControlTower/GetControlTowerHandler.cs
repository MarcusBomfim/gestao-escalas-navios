using PortManagement.Application.Common;

namespace PortManagement.Application.ControlTower;

public sealed class GetControlTowerHandler(
    IControlTowerRepository repository,
    TimeProvider timeProvider)
{
    public async Task<Result<ControlTowerResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var snapshot = await repository.GetSnapshotAsync(now, cancellationToken);
        return Result.Success(ControlTowerEvaluator.Evaluate(snapshot, now));
    }
}
