using PortManagement.Application.Common;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.Application.Planning;

public sealed record BerthWindowRevisionResponse(
    Guid PreviousBerthId,
    Guid NewBerthId,
    DateTimeOffset PreviousStartsAtUtc,
    DateTimeOffset PreviousEndsAtUtc,
    DateTimeOffset NewStartsAtUtc,
    DateTimeOffset NewEndsAtUtc,
    string ChangedBy,
    string Reason,
    DateTimeOffset ChangedAtUtc);

public sealed record BerthWindowResponse(
    Guid Id,
    Guid PortCallId,
    string PortCallPublicCode,
    Guid VesselId,
    string VesselName,
    Guid PortId,
    string PortName,
    Guid TerminalId,
    string TerminalName,
    Guid BerthId,
    string BerthCode,
    string BerthName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    BerthWindowStatus Status,
    string RequestedBy,
    string? LastChangedBy,
    string? LastChangeReason,
    long Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyCollection<BerthWindowRevisionResponse> Revisions);

public sealed record PortCallBerthWindowResponse(BerthWindowResponse? Window);

public sealed record BerthPlanningReference(Berth Berth, Guid PortId);

public sealed record PortCallPlanningReference(PortCall PortCall, Vessel Vessel);

public sealed record RequestBerthWindowCommand(
    string PortCallPublicCode,
    Guid BerthId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    long ExpectedPortCallVersion,
    string RequestedBy);

public sealed record ReprogramBerthWindowCommand(
    string PortCallPublicCode,
    Guid BerthId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    long ExpectedWindowVersion,
    string ChangedBy,
    string Reason);

public sealed record ChangeBerthWindowStatusCommand(
    string PortCallPublicCode,
    long ExpectedWindowVersion,
    string ChangedBy,
    string? Reason);

public sealed record ListBerthWindowsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? PortId = null,
    Guid? BerthId = null,
    BerthWindowStatus? Status = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null);

public interface IBerthWindowRepository
{
    Task<PortCallPlanningReference?> FindPortCallForPlanningAsync(
        string publicCode,
        CancellationToken cancellationToken);

    Task<BerthPlanningReference?> FindBerthForPlanningAsync(
        Guid berthId,
        CancellationToken cancellationToken);

    Task<BerthWindow?> FindActiveTrackedByPortCallAsync(
        Guid portCallId,
        CancellationToken cancellationToken);

    Task<bool> ConfirmedOverlapExistsAsync(
        Guid berthId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludingWindowId,
        CancellationToken cancellationToken);

    Task AddAsync(BerthWindow window, CancellationToken cancellationToken);

    Task<BerthWindowResponse?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<BerthWindowResponse?> GetActiveDetailsByPublicCodeAsync(
        string publicCode,
        CancellationToken cancellationToken);

    Task<PagedResult<BerthWindowResponse>> ListAsync(
        ListBerthWindowsQuery query,
        CancellationToken cancellationToken);
}
