using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Operations;
using PortManagement.Domain.Operations;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class OperationalExecutionRepository(PortManagementDbContext database)
    : IOperationalExecutionRepository
{
    public Task<PortCall?> FindPortCallTrackedAsync(string publicCode, CancellationToken cancellationToken) =>
        database.PortCalls.SingleOrDefaultAsync(
            portCall => portCall.PublicCode == publicCode,
            cancellationToken);

    public Task<CargoOperation?> FindCargoOperationTrackedAsync(
        string publicCode,
        Guid cargoOperationId,
        CancellationToken cancellationToken) =>
        database.CargoOperations
            .Include(operation => operation.PortCall)
            .SingleOrDefaultAsync(
                operation => operation.Id == cargoOperationId
                    && operation.PortCall.PublicCode == publicCode,
                cancellationToken);

    public Task<DateTimeOffset?> GetLatestActualEventAtAsync(
        Guid portCallId,
        CancellationToken cancellationToken) =>
        database.PortCallEvents
            .Where(portCallEvent => portCallEvent.PortCallId == portCallId
                && portCallEvent.Classifier == TemporalClassifier.Actual)
            .MaxAsync(portCallEvent => (DateTimeOffset?)portCallEvent.OccursAtUtc, cancellationToken);

    public Task<bool> HasCargoOperationsAsync(Guid portCallId, CancellationToken cancellationToken) =>
        database.CargoOperations.AnyAsync(
            operation => operation.PortCallId == portCallId,
            cancellationToken);

    public async Task<bool> AreAllCargoOperationsCompletedAsync(
        Guid portCallId,
        CancellationToken cancellationToken)
    {
        var operations = database.CargoOperations.Where(operation => operation.PortCallId == portCallId);
        return await operations.AnyAsync(cancellationToken)
            && !await operations.AnyAsync(operation => operation.ActualEndAtUtc == null, cancellationToken);
    }

    public async Task<OperationalExecutionResponse?> GetAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var portCall = await database.PortCalls
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.PublicCode == publicCode, cancellationToken);
        if (portCall is null)
        {
            return null;
        }

        var events = await database.PortCallEvents
            .AsNoTracking()
            .Where(portCallEvent => portCallEvent.PortCallId == portCall.Id
                && portCallEvent.Classifier == TemporalClassifier.Actual)
            .OrderBy(portCallEvent => portCallEvent.OccursAtUtc)
            .ThenBy(portCallEvent => portCallEvent.RecordedAtUtc)
            .Select(portCallEvent => new OperationalEventResponse(
                portCallEvent.Id,
                portCallEvent.Phase,
                portCallEvent.Action,
                portCallEvent.OccursAtUtc,
                portCallEvent.Source,
                portCallEvent.RecordedBy,
                portCallEvent.RecordedAtUtc))
            .ToArrayAsync(cancellationToken);

        var cargoEntities = await database.CargoOperations
            .AsNoTracking()
            .Where(operation => operation.PortCallId == portCall.Id)
            .OrderBy(operation => operation.PlannedStartAtUtc)
            .ThenBy(operation => operation.CargoType)
            .ToArrayAsync(cancellationToken);
        var cargoOperations = cargoEntities
            .Select(CreateCargoOperationHandler.ToResponse)
            .ToArray();

        var operationStart = FindEvent(events, PortCallEventPhase.CargoOperation, PortCallEventAction.Start);
        var operationEnd = FindEvent(events, PortCallEventPhase.CargoOperation, PortCallEventAction.Completion);
        var operationHours = DurationHours(operationStart, operationEnd);
        var cargoSummaries = cargoEntities
            .GroupBy(operation => operation.QuantityUnit)
            .Select(group =>
            {
                var actual = group.Sum(operation => operation.ActualQuantity ?? 0);
                var productivity = operationHours is > 0 && actual > 0
                    ? decimal.Round(actual / (decimal)operationHours.Value, 2)
                    : (decimal?)null;
                return new CargoUnitSummaryResponse(
                    group.Key,
                    group.Sum(operation => operation.PlannedQuantity),
                    actual,
                    productivity);
            })
            .OrderBy(summary => summary.QuantityUnit)
            .ToArray();

        var now = DateTimeOffset.UtcNow;
        var arrival = FindEvent(events, PortCallEventPhase.Anchorage, PortCallEventAction.Arrival);
        var departure = FindEvent(events, PortCallEventPhase.Departure, PortCallEventAction.Departure);
        var berthed = FindEvent(events, PortCallEventPhase.Berth, PortCallEventAction.Completion);
        var unberthed = FindEvent(events, PortCallEventPhase.Departure, PortCallEventAction.Start);

        return new OperationalExecutionResponse(
            portCall.Id,
            portCall.PublicCode,
            portCall.Status,
            portCall.Version,
            OperationalMilestoneRules.NextFor(portCall.Status),
            events,
            cargoOperations,
            new OperationalKpiResponse(
                DurationHours(arrival, departure ?? (arrival.HasValue ? now : null)),
                DurationHours(berthed, unberthed ?? (berthed.HasValue ? now : null)),
                DurationHours(operationStart, operationEnd ?? (operationStart.HasValue ? now : null)),
                cargoSummaries));
    }

    public Task AddEventAsync(PortCallEvent portCallEvent, CancellationToken cancellationToken) =>
        database.PortCallEvents.AddAsync(portCallEvent, cancellationToken).AsTask();

    public Task AddCargoOperationAsync(CargoOperation cargoOperation, CancellationToken cancellationToken) =>
        database.CargoOperations.AddAsync(cargoOperation, cancellationToken).AsTask();

    private static DateTimeOffset? FindEvent(
        IEnumerable<OperationalEventResponse> events,
        PortCallEventPhase phase,
        PortCallEventAction action) =>
        events.FirstOrDefault(item => item.Phase == phase && item.Action == action)?.OccursAtUtc;

    private static double? DurationHours(DateTimeOffset? start, DateTimeOffset? end) =>
        start.HasValue && end.HasValue && end >= start
            ? Math.Round((end.Value - start.Value).TotalHours, 2)
            : null;
}
