using PortManagement.Application.Common;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Application.PortCalls;

public sealed record PortCallStatusHistoryResponse(
    PortCallStatus PreviousStatus,
    PortCallStatus NewStatus,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc,
    string? Reason);

public sealed record PortCallResponse(
    Guid Id,
    string PublicCode,
    Guid VesselId,
    string VesselName,
    Guid PortId,
    string PortName,
    PortCallPurpose Purpose,
    PortCallStatus Status,
    string? VoyageNumber,
    string? PreviousPortUnLocode,
    string? NextPortUnLocode,
    Guid? PlannedTerminalId,
    string? PlannedTerminalName,
    Guid? PlannedBerthId,
    string? PlannedBerthName,
    long Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    IReadOnlyCollection<PortCallStatusHistoryResponse> StatusHistory);

public sealed record CreatePortCallCommand(
    Guid VesselId,
    Guid PortId,
    PortCallPurpose Purpose,
    string IdempotencyKey,
    string? VoyageNumber,
    string? PreviousPortUnLocode,
    string? NextPortUnLocode);

public sealed record CreatePortCallResponse(
    PortCallResponse PortCall,
    bool Created);

public sealed record ListPortCallsQuery(
    int Page = 1,
    int PageSize = 20,
    PortCallStatus? Status = null,
    Guid? PortId = null,
    string? Search = null);

public sealed record TransitionPortCallCommand(
    string PublicCode,
    PortCallStatus NewStatus,
    long ExpectedVersion,
    string ChangedBy,
    string? Reason);

public interface IPortCallRepository
{
    Task<bool> ActiveVesselExistsAsync(Guid vesselId, CancellationToken cancellationToken);

    Task<bool> ActivePortExistsAsync(Guid portId, CancellationToken cancellationToken);

    Task<OrganizationType?> GetActiveOrganizationTypeAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    Task<PortCall?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<PortCall?> FindTrackedByPublicCodeAsync(
        string publicCode,
        CancellationToken cancellationToken);

    Task<PortCallResponse?> GetDetailsByPublicCodeAsync(
        string publicCode,
        CancellationToken cancellationToken);

    Task<PagedResult<PortCallResponse>> ListAsync(
        ListPortCallsQuery query,
        CancellationToken cancellationToken);

    Task AddAsync(PortCall portCall, CancellationToken cancellationToken);
}
