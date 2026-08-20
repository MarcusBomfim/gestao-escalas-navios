using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Common;
using PortManagement.Application.Vessels;
using PortManagement.Domain.Vessels;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class VesselRepository(PortManagementDbContext database) : IVesselRepository
{
    public Task<bool> ActiveImoExistsAsync(
        ImoNumber imoNumber,
        CancellationToken cancellationToken) =>
        database.Vessels.AnyAsync(
            vessel => vessel.IsActive && vessel.ImoNumber == imoNumber,
            cancellationToken);

    public async Task AddAsync(Vessel vessel, CancellationToken cancellationToken)
    {
        await database.Vessels.AddAsync(vessel, cancellationToken);
    }

    public Task<Vessel?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        database.Vessels
            .AsNoTracking()
            .SingleOrDefaultAsync(vessel => vessel.Id == id, cancellationToken);

    public async Task<PagedResult<VesselResponse>> ListAsync(
        ListVesselsQuery query,
        CancellationToken cancellationToken)
    {
        var vessels = database.Vessels.AsNoTracking();

        if (query.ActiveOnly)
        {
            vessels = vessels.Where(vessel => vessel.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            vessels = vessels.Where(vessel =>
                EF.Functions.ILike(vessel.Name, pattern)
                || (vessel.CallSign != null && EF.Functions.ILike(vessel.CallSign, pattern)));
        }

        var totalItems = await vessels.CountAsync(cancellationToken);
        var page = await vessels
            .OrderBy(vessel => vessel.Name)
            .ThenBy(vessel => vessel.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<VesselResponse>(
            page.Select(ToResponse).ToArray(),
            query.Page,
            query.PageSize,
            totalItems);
    }

    private static VesselResponse ToResponse(Vessel vessel) => new(
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
