using PortManagement.Domain.Ports;
using PortManagement.Domain.Vessels;

namespace PortManagement.Application.ReferenceData;

public sealed record BerthReferenceResponse(
    Guid Id,
    string Code,
    string Name,
    decimal UsefulLengthMeters,
    decimal MaximumBeamMeters,
    decimal MaximumDraftMeters,
    VesselType[] SupportedVesselTypes,
    BerthStatus Status);

public sealed record TerminalReferenceResponse(
    Guid Id,
    string Code,
    string Name,
    string TimeZoneId,
    IReadOnlyCollection<BerthReferenceResponse> Berths);

public sealed record PortReferenceResponse(
    Guid Id,
    string Name,
    string UnLocode,
    string CountryCode,
    string TimeZoneId,
    IReadOnlyCollection<TerminalReferenceResponse> Terminals);

public interface IPortStructureRepository
{
    Task<IReadOnlyCollection<PortReferenceResponse>> ListActiveAsync(
        CancellationToken cancellationToken);
}
