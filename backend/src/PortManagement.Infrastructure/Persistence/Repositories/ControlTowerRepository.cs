using Microsoft.EntityFrameworkCore;
using PortManagement.Application.ControlTower;
using PortManagement.Domain.Operations;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class ControlTowerRepository(PortManagementDbContext database) : IControlTowerRepository
{
    public async Task<ControlTowerSnapshot> GetSnapshotAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var portCalls = await database.PortCalls
            .AsNoTracking()
            .Include(portCall => portCall.Vessel)
            .Include(portCall => portCall.Port)
            .Where(portCall => portCall.Status != PortCallStatus.Closed
                && portCall.Status != PortCallStatus.Cancelled)
            .OrderBy(portCall => portCall.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        var portCallIds = portCalls.Select(portCall => portCall.Id).ToArray();

        var windows = await database.BerthWindows
            .AsNoTracking()
            .Include(window => window.Berth)
                .ThenInclude(berth => berth.Terminal)
            .Where(window => portCallIds.Contains(window.PortCallId)
                && (window.Status == BerthWindowStatus.Requested
                    || window.Status == BerthWindowStatus.Confirmed))
            .ToArrayAsync(cancellationToken);
        var events = await database.PortCallEvents
            .AsNoTracking()
            .Where(portCallEvent => portCallIds.Contains(portCallEvent.PortCallId)
                && portCallEvent.Classifier == TemporalClassifier.Actual)
            .ToArrayAsync(cancellationToken);
        var cargoOperations = await database.CargoOperations
            .AsNoTracking()
            .Where(operation => portCallIds.Contains(operation.PortCallId))
            .ToArrayAsync(cancellationToken);
        var totalBerths = await database.Berths.CountAsync(cancellationToken);

        var snapshots = portCalls.Select(portCall =>
        {
            var window = windows.SingleOrDefault(item => item.PortCallId == portCall.Id);
            var callEvents = events.Where(item => item.PortCallId == portCall.Id).ToArray();
            var callCargo = cargoOperations.Where(item => item.PortCallId == portCall.Id).ToArray();
            var overdueCargo = callCargo
                .Where(operation => !operation.ActualEndAtUtc.HasValue
                    && operation.PlannedEndAtUtc.HasValue
                    && operation.PlannedEndAtUtc.Value < nowUtc)
                .ToArray();
            var lastActivity = new DateTimeOffset?[]
                {
                    portCall.UpdatedAtUtc,
                    callEvents.Length == 0 ? null : callEvents.Max(item => item.OccursAtUtc),
                    callCargo.Length == 0 ? null : callCargo.Max(item => item.UpdatedAtUtc)
                }
                .Where(item => item.HasValue)
                .Max();

            return new ControlTowerCallSnapshot(
                portCall.Id,
                portCall.PublicCode,
                portCall.Vessel.Name,
                portCall.Status,
                portCall.Port.Name,
                window?.Berth.Terminal.Name,
                window?.Berth.Name,
                window?.Status,
                window?.StartsAtUtc,
                window?.EndsAtUtc,
                FindEvent(callEvents, PortCallEventPhase.Anchorage, PortCallEventAction.Arrival),
                FindEvent(callEvents, PortCallEventPhase.Berth, PortCallEventAction.Completion),
                FindEvent(callEvents, PortCallEventPhase.CargoOperation, PortCallEventAction.Start),
                FindEvent(callEvents, PortCallEventPhase.CargoOperation, PortCallEventAction.Completion),
                FindEvent(callEvents, PortCallEventPhase.Departure, PortCallEventAction.Start),
                lastActivity,
                callCargo.Count(operation => !operation.ActualEndAtUtc.HasValue),
                overdueCargo.Length,
                overdueCargo.Length == 0
                    ? null
                    : overdueCargo.Min(operation => operation.PlannedEndAtUtc));
        }).ToArray();

        return new ControlTowerSnapshot(totalBerths, snapshots);
    }

    private static DateTimeOffset? FindEvent(
        IEnumerable<PortCallEvent> events,
        PortCallEventPhase phase,
        PortCallEventAction action) =>
        events
            .Where(item => item.Phase == phase && item.Action == action)
            .Select(item => (DateTimeOffset?)item.OccursAtUtc)
            .Min();
}
