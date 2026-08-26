using PortManagement.Application.Operations;
using PortManagement.Domain.Operations;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.ControlTower;

public enum OperationalAlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum OperationalAlertType
{
    MissingBerthPlan,
    PendingBerthConfirmation,
    ArrivalDelay,
    BerthOverstay,
    CargoDelay,
    ScheduleDeviation,
    StaleOperationalUpdate
}

public sealed record ControlTowerCallSnapshot(
    Guid PortCallId,
    string PublicCode,
    string VesselName,
    PortCallStatus Status,
    string PortName,
    string? TerminalName,
    string? BerthName,
    BerthWindowStatus? WindowStatus,
    DateTimeOffset? WindowStartsAtUtc,
    DateTimeOffset? WindowEndsAtUtc,
    DateTimeOffset? ArrivedAtAnchorageUtc,
    DateTimeOffset? BerthedAtUtc,
    DateTimeOffset? CargoStartedAtUtc,
    DateTimeOffset? CargoCompletedAtUtc,
    DateTimeOffset? UnberthedAtUtc,
    DateTimeOffset? LastActivityAtUtc,
    int IncompleteCargoOperations,
    int OverdueCargoOperations,
    DateTimeOffset? OldestOverdueCargoEndUtc);

public sealed record ControlTowerSnapshot(
    int TotalBerths,
    IReadOnlyCollection<ControlTowerCallSnapshot> Calls);

public sealed record OperationalAlertResponse(
    string Id,
    Guid PortCallId,
    string PortCallPublicCode,
    string VesselName,
    OperationalAlertSeverity Severity,
    OperationalAlertType Type,
    string Title,
    string Description,
    int? DeviationMinutes,
    DateTimeOffset DetectedAtUtc,
    string ActionPath);

public sealed record ControlTowerCallResponse(
    Guid Id,
    string PublicCode,
    string VesselName,
    PortCallStatus Status,
    string PortName,
    string? TerminalName,
    string? BerthName,
    DateTimeOffset? WindowStartsAtUtc,
    DateTimeOffset? WindowEndsAtUtc,
    DateTimeOffset? LastActivityAtUtc,
    OperationalMilestone? NextMilestone,
    int AlertCount,
    OperationalAlertSeverity? HighestAlertSeverity);

public sealed record ControlTowerSummaryResponse(
    int ActivePortCalls,
    int InOperation,
    int CallsRequiringAttention,
    int CriticalAlerts,
    int OccupiedBerths,
    int TotalBerths,
    decimal BerthOccupancyPercent,
    decimal ScheduleCompliancePercent);

public enum VesselNavigationState
{
    AwaitingSchedule,
    Approaching,
    Anchored,
    Manoeuvring,
    Berthed,
    Operating,
    ReadyToSail,
    Departing
}

public sealed record VesselPositionResponse(
    Guid PortCallId,
    string PortCallPublicCode,
    string VesselName,
    string PortName,
    string? TerminalName,
    string? BerthName,
    PortCallStatus PortCallStatus,
    VesselNavigationState NavigationState,
    decimal XPercent,
    decimal YPercent,
    decimal SpeedKnots,
    int CourseDegrees,
    DateTimeOffset ObservedAtUtc,
    bool IsSimulated);

public sealed record VesselTrafficResponse(
    DateTimeOffset GeneratedAtUtc,
    string CoverageLabel,
    bool IsSimulated,
    IReadOnlyCollection<VesselPositionResponse> Positions);

public sealed record ControlTowerResponse(
    DateTimeOffset GeneratedAtUtc,
    ControlTowerSummaryResponse Summary,
    IReadOnlyCollection<OperationalAlertResponse> Alerts,
    IReadOnlyCollection<ControlTowerCallResponse> Calls,
    VesselTrafficResponse Traffic);

public interface IControlTowerRepository
{
    Task<ControlTowerSnapshot> GetSnapshotAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);
}
