using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Common;
using PortManagement.Application.PortCalls;
using PortManagement.Application.Security;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.PortCalls;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class PortCallRepository(
    PortManagementDbContext database,
    IUserDataScope dataScope) : IPortCallRepository
{
    public Task<bool> ActiveVesselExistsAsync(Guid vesselId, CancellationToken cancellationToken) =>
        database.Vessels.AnyAsync(
            vessel => vessel.Id == vesselId && vessel.IsActive,
            cancellationToken);

    public Task<bool> ActivePortExistsAsync(Guid portId, CancellationToken cancellationToken) =>
        database.Ports.AnyAsync(
            port => port.Id == portId && port.IsActive,
            cancellationToken);

    public Task<OrganizationType?> GetActiveOrganizationTypeAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        database.Organizations
            .Where(organization => organization.Id == organizationId && organization.IsActive)
            .Select(organization => (OrganizationType?)organization.Type)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<PortCall?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        database.PortCalls
            .ApplyOrganizationScope(dataScope)
            .AsNoTracking()
            .SingleOrDefaultAsync(
                portCall => portCall.IdempotencyKey == idempotencyKey,
                cancellationToken);

    public Task<PortCall?> FindTrackedByPublicCodeAsync(
        string publicCode,
        CancellationToken cancellationToken) =>
        database.PortCalls
            .ApplyOrganizationScope(dataScope)
            .SingleOrDefaultAsync(
            portCall => portCall.PublicCode == publicCode,
            cancellationToken);

    public async Task<PortCallResponse?> GetDetailsByPublicCodeAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var portCall = await DetailsQuery(includeHistory: true)
            .SingleOrDefaultAsync(
                item => item.PublicCode == publicCode,
                cancellationToken);

        return portCall is null ? null : ToResponse(portCall, includeHistory: true);
    }

    public async Task<PagedResult<PortCallResponse>> ListAsync(
        ListPortCallsQuery query,
        CancellationToken cancellationToken)
    {
        var portCalls = DetailsQuery(includeHistory: false);

        if (query.Status.HasValue)
        {
            portCalls = portCalls.Where(portCall => portCall.Status == query.Status.Value);
        }

        if (query.PortId.HasValue)
        {
            portCalls = portCalls.Where(portCall => portCall.PortId == query.PortId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            portCalls = portCalls.Where(portCall =>
                EF.Functions.ILike(portCall.PublicCode, pattern)
                || EF.Functions.ILike(portCall.Vessel.Name, pattern)
                || (portCall.VoyageNumber != null && EF.Functions.ILike(portCall.VoyageNumber, pattern)));
        }

        var totalItems = await portCalls.CountAsync(cancellationToken);
        var page = await portCalls
            .OrderByDescending(portCall => portCall.CreatedAtUtc)
            .ThenBy(portCall => portCall.PublicCode)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PortCallResponse>(
            page.Select(portCall => ToResponse(portCall, includeHistory: false)).ToArray(),
            query.Page,
            query.PageSize,
            totalItems);
    }

    public async Task AddAsync(PortCall portCall, CancellationToken cancellationToken)
    {
        await database.PortCalls.AddAsync(portCall, cancellationToken);
    }

    private IQueryable<PortCall> DetailsQuery(bool includeHistory)
    {
        IQueryable<PortCall> query = database.PortCalls
            .ApplyOrganizationScope(dataScope)
            .AsNoTracking()
            .Include(portCall => portCall.Vessel)
            .Include(portCall => portCall.Port)
            .Include(portCall => portCall.PlannedTerminal)
            .Include(portCall => portCall.PlannedBerth);

        if (includeHistory)
        {
            query = query.Include(portCall => portCall.StatusHistory);
        }

        return query;
    }

    private static PortCallResponse ToResponse(PortCall portCall, bool includeHistory) => new(
        portCall.Id,
        portCall.PublicCode,
        portCall.VesselId,
        portCall.Vessel.Name,
        portCall.PortId,
        portCall.Port.Name,
        portCall.Purpose,
        portCall.Status,
        portCall.VoyageNumber,
        portCall.PreviousPortUnLocode,
        portCall.NextPortUnLocode,
        portCall.PlannedTerminalId,
        portCall.PlannedTerminal?.Name,
        portCall.PlannedBerthId,
        portCall.PlannedBerth?.Name,
        portCall.Version,
        portCall.CreatedAtUtc,
        portCall.UpdatedAtUtc,
        portCall.ClosedAtUtc,
        includeHistory
            ? portCall.StatusHistory
                .OrderBy(history => history.ChangedAtUtc)
                .Select(history => new PortCallStatusHistoryResponse(
                    history.PreviousStatus,
                    history.NewStatus,
                    history.ChangedBy,
                    history.ChangedAtUtc,
                    history.Reason))
                .ToArray()
            : []);
}
