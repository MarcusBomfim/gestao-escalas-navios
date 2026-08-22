using PortManagement.Application.Common;
using PortManagement.Domain.Operations;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.Operations;

public sealed record OperationalEventResponse(
    Guid Id,
    PortCallEventPhase Phase,
    PortCallEventAction Action,
    DateTimeOffset OccursAtUtc,
    string Source,
    string RecordedBy,
    DateTimeOffset RecordedAtUtc);

public sealed record CargoOperationResponse(
    Guid Id,
    CargoOperationDirection Direction,
    string CargoType,
    decimal PlannedQuantity,
    decimal? ActualQuantity,
    CargoQuantityUnit QuantityUnit,
    bool IsDangerousCargo,
    string? DangerousCargoClassification,
    DateTimeOffset? PlannedStartAtUtc,
    DateTimeOffset? PlannedEndAtUtc,
    DateTimeOffset? ActualStartAtUtc,
    DateTimeOffset? ActualEndAtUtc,
    long Version,
    string Status);

public sealed record CargoUnitSummaryResponse(
    CargoQuantityUnit QuantityUnit,
    decimal PlannedQuantity,
    decimal ActualQuantity,
    decimal? ProductivityPerHour);

public sealed record OperationalKpiResponse(
    double? PortStayHours,
    double? BerthStayHours,
    double? CargoOperationHours,
    IReadOnlyCollection<CargoUnitSummaryResponse> CargoSummaries);

public sealed record OperationalExecutionResponse(
    Guid PortCallId,
    string PortCallPublicCode,
    PortCallStatus PortCallStatus,
    long PortCallVersion,
    OperationalMilestone? NextMilestone,
    IReadOnlyCollection<OperationalEventResponse> Events,
    IReadOnlyCollection<CargoOperationResponse> CargoOperations,
    OperationalKpiResponse Kpis);

public sealed record RecordOperationalMilestoneCommand(
    string PublicCode,
    OperationalMilestone Milestone,
    DateTimeOffset OccursAtUtc,
    long ExpectedPortCallVersion,
    string Source,
    string RecordedBy);

public sealed record CreateCargoOperationCommand(
    string PublicCode,
    CargoOperationDirection Direction,
    string CargoType,
    decimal PlannedQuantity,
    CargoQuantityUnit QuantityUnit,
    bool IsDangerousCargo,
    string? DangerousCargoClassification,
    DateTimeOffset PlannedStartAtUtc,
    DateTimeOffset PlannedEndAtUtc,
    long ExpectedPortCallVersion);

public sealed record StartCargoOperationCommand(
    string PublicCode,
    Guid CargoOperationId,
    DateTimeOffset StartedAtUtc,
    long ExpectedVersion);

public sealed record CompleteCargoOperationCommand(
    string PublicCode,
    Guid CargoOperationId,
    decimal ActualQuantity,
    DateTimeOffset CompletedAtUtc,
    long ExpectedVersion);

public interface IOperationalExecutionRepository
{
    Task<PortCall?> FindPortCallTrackedAsync(string publicCode, CancellationToken cancellationToken);

    Task<CargoOperation?> FindCargoOperationTrackedAsync(
        string publicCode,
        Guid cargoOperationId,
        CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLatestActualEventAtAsync(
        Guid portCallId,
        CancellationToken cancellationToken);

    Task<bool> HasCargoOperationsAsync(Guid portCallId, CancellationToken cancellationToken);

    Task<bool> AreAllCargoOperationsCompletedAsync(Guid portCallId, CancellationToken cancellationToken);

    Task<OperationalExecutionResponse?> GetAsync(string publicCode, CancellationToken cancellationToken);

    Task AddEventAsync(PortCallEvent portCallEvent, CancellationToken cancellationToken);

    Task AddCargoOperationAsync(CargoOperation cargoOperation, CancellationToken cancellationToken);
}
