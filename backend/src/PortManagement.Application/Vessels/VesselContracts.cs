using PortManagement.Application.Common;
using PortManagement.Domain.Vessels;

namespace PortManagement.Application.Vessels;

public sealed record VesselResponse(
    Guid Id,
    string Name,
    string? ImoNumber,
    string FlagCode,
    VesselType Type,
    decimal LengthOverallMeters,
    decimal BeamMeters,
    decimal MaximumDraftMeters,
    string? CallSign,
    string? Mmsi,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record RegisterVesselCommand(
    string Name,
    string? ImoNumber,
    string FlagCode,
    VesselType Type,
    decimal LengthOverallMeters,
    decimal BeamMeters,
    decimal MaximumDraftMeters,
    string? CallSign,
    string? Mmsi);

public sealed record UpdateVesselCommand(
    Guid Id,
    string Name,
    string? ImoNumber,
    string FlagCode,
    VesselType Type,
    decimal LengthOverallMeters,
    decimal BeamMeters,
    decimal MaximumDraftMeters,
    string? CallSign,
    string? Mmsi);

public sealed record ListVesselsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool ActiveOnly = true);

public interface IVesselRepository
{
    Task<bool> ActiveImoExistsAsync(
        ImoNumber imoNumber,
        Guid? excludingVesselId,
        CancellationToken cancellationToken);

    Task AddAsync(Vessel vessel, CancellationToken cancellationToken);

    Task<Vessel?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Vessel?> FindTrackedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<VesselResponse>> ListAsync(
        ListVesselsQuery query,
        CancellationToken cancellationToken);
}

internal static class VesselMapping
{
    public static VesselResponse ToResponse(this Vessel vessel) => new(
        vessel.Id,
        vessel.Name,
        vessel.ImoNumber?.Value,
        vessel.FlagCode,
        vessel.Type,
        vessel.LengthOverallMeters,
        vessel.BeamMeters,
        vessel.MaximumDraftMeters,
        vessel.CallSign,
        vessel.Mmsi,
        vessel.IsActive,
        vessel.CreatedAtUtc,
        vessel.UpdatedAtUtc);
}
