using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.Application.Administration;

public sealed record ListOrganizationsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    OrganizationType? Type = null,
    bool? IsActive = null);

public sealed record OrganizationAdminResponse(
    Guid Id,
    string Name,
    string RegistrationNumber,
    OrganizationType Type,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record CreateOrganizationCommand(
    string Name,
    string RegistrationNumber,
    OrganizationType Type);

public sealed record UpdateOrganizationCommand(
    Guid Id,
    string Name,
    string RegistrationNumber,
    OrganizationType Type,
    bool IsActive,
    DateTimeOffset ExpectedUpdatedAtUtc);

public sealed record BerthAdminResponse(
    Guid Id,
    Guid TerminalId,
    string Code,
    string Name,
    decimal UsefulLengthMeters,
    decimal MaximumBeamMeters,
    decimal MaximumDraftMeters,
    VesselType[] SupportedVesselTypes,
    BerthStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record TerminalAdminResponse(
    Guid Id,
    Guid PortId,
    string Code,
    string Name,
    string TimeZoneId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyCollection<BerthAdminResponse> Berths);

public sealed record PortAdminResponse(
    Guid Id,
    string Name,
    string UnLocode,
    string CountryCode,
    string TimeZoneId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyCollection<TerminalAdminResponse> Terminals);

public sealed record CreatePortCommand(
    string Name,
    string UnLocode,
    string CountryCode,
    string TimeZoneId);

public sealed record UpdatePortCommand(
    Guid Id,
    string Name,
    string UnLocode,
    string CountryCode,
    string TimeZoneId,
    bool IsActive,
    DateTimeOffset ExpectedUpdatedAtUtc);

public sealed record CreateTerminalCommand(
    Guid PortId,
    string Code,
    string Name,
    string TimeZoneId);

public sealed record UpdateTerminalCommand(
    Guid Id,
    string Code,
    string Name,
    string TimeZoneId,
    bool IsActive,
    DateTimeOffset ExpectedUpdatedAtUtc);

public sealed record CreateBerthCommand(
    Guid TerminalId,
    string Code,
    string Name,
    decimal UsefulLengthMeters,
    decimal MaximumBeamMeters,
    decimal MaximumDraftMeters,
    VesselType[] SupportedVesselTypes);

public sealed record UpdateBerthCommand(
    Guid Id,
    string Code,
    string Name,
    decimal UsefulLengthMeters,
    decimal MaximumBeamMeters,
    decimal MaximumDraftMeters,
    VesselType[] SupportedVesselTypes,
    BerthStatus Status,
    DateTimeOffset ExpectedUpdatedAtUtc);

public interface IMasterDataRepository
{
    Task<PagedResult<OrganizationAdminResponse>> ListOrganizationsAsync(
        ListOrganizationsQuery query,
        CancellationToken cancellationToken);

    Task<bool> OrganizationRegistrationExistsAsync(
        string registrationNumber,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> OrganizationHasActiveUsersAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    Task AddOrganizationAsync(
        Organization organization,
        CancellationToken cancellationToken);

    Task<Organization?> FindOrganizationAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PortAdminResponse>> ListPortStructureAsync(
        CancellationToken cancellationToken);

    Task<bool> PortUnLocodeExistsAsync(
        string unLocode,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> PortHasActiveTerminalsAsync(
        Guid portId,
        CancellationToken cancellationToken);

    Task AddPortAsync(Port port, CancellationToken cancellationToken);

    Task<Port?> FindPortAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> TerminalCodeExistsAsync(
        Guid portId,
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> TerminalHasAvailableBerthsAsync(
        Guid terminalId,
        CancellationToken cancellationToken);

    Task AddTerminalAsync(Terminal terminal, CancellationToken cancellationToken);

    Task<Terminal?> FindTerminalAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> BerthCodeExistsAsync(
        Guid terminalId,
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> BerthHasOpenWindowsAsync(
        Guid berthId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken);

    Task AddBerthAsync(Berth berth, CancellationToken cancellationToken);

    Task<Berth?> FindBerthAsync(Guid id, CancellationToken cancellationToken);

    void UseExpectedUpdatedAt(AuditableEntity entity, DateTimeOffset expectedUpdatedAtUtc);
}

public static class MasterDataMapping
{
    public static OrganizationAdminResponse ToAdminResponse(this Organization organization) => new(
        organization.Id,
        organization.Name,
        organization.RegistrationNumber,
        organization.Type,
        organization.IsActive,
        organization.CreatedAtUtc,
        organization.UpdatedAtUtc);

    public static BerthAdminResponse ToAdminResponse(this Berth berth) => new(
        berth.Id,
        berth.TerminalId,
        berth.Code,
        berth.Name,
        berth.UsefulLengthMeters,
        berth.MaximumBeamMeters,
        berth.MaximumDraftMeters,
        berth.SupportedVesselTypes,
        berth.Status,
        berth.CreatedAtUtc,
        berth.UpdatedAtUtc);

    public static TerminalAdminResponse ToAdminResponse(
        this Terminal terminal,
        IReadOnlyCollection<BerthAdminResponse> berths) => new(
        terminal.Id,
        terminal.PortId,
        terminal.Code,
        terminal.Name,
        terminal.TimeZoneId,
        terminal.IsActive,
        terminal.CreatedAtUtc,
        terminal.UpdatedAtUtc,
        berths);

    public static PortAdminResponse ToAdminResponse(
        this Port port,
        IReadOnlyCollection<TerminalAdminResponse> terminals) => new(
        port.Id,
        port.Name,
        port.UnLocode,
        port.CountryCode,
        port.TimeZoneId,
        port.IsActive,
        port.CreatedAtUtc,
        port.UpdatedAtUtc,
        terminals);
}
