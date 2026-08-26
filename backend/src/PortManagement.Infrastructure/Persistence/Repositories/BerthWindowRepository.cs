using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Common;
using PortManagement.Application.Planning;
using PortManagement.Application.Security;
using PortManagement.Domain.Planning;
using PortManagement.Domain.PortCalls;
using PortManagement.Domain.Ports;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class BerthWindowRepository(
    PortManagementDbContext database,
    IUserDataScope dataScope) : IBerthWindowRepository
{
    public async Task<PortCallPlanningReference?> FindPortCallForPlanningAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var portCall = await database.PortCalls
            .ApplyOrganizationScope(dataScope)
            .Include(portCall => portCall.Vessel)
            .SingleOrDefaultAsync(portCall => portCall.PublicCode == publicCode, cancellationToken);

        return portCall is null ? null : new PortCallPlanningReference(portCall, portCall.Vessel);
    }

    public async Task<BerthPlanningReference?> FindBerthForPlanningAsync(
        Guid berthId,
        CancellationToken cancellationToken)
    {
        var berth = await database.Berths
            .Include(berth => berth.Terminal)
            .SingleOrDefaultAsync(berth => berth.Id == berthId, cancellationToken);

        return berth is null ? null : new BerthPlanningReference(berth, berth.Terminal.PortId);
    }

    public Task<BerthWindow?> FindActiveTrackedByPortCallAsync(
        Guid portCallId,
        CancellationToken cancellationToken) =>
        database.BerthWindows
            .ApplyOrganizationScope(dataScope)
            .SingleOrDefaultAsync(
            window => window.PortCallId == portCallId
                && (window.Status == BerthWindowStatus.Requested
                    || window.Status == BerthWindowStatus.Confirmed),
            cancellationToken);

    public Task<bool> ConfirmedOverlapExistsAsync(
        Guid berthId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludingWindowId,
        CancellationToken cancellationToken) =>
        database.BerthWindows.AnyAsync(
            window => window.BerthId == berthId
                && window.Status == BerthWindowStatus.Confirmed
                && window.StartsAtUtc < endsAtUtc
                && window.EndsAtUtc > startsAtUtc
                && (!excludingWindowId.HasValue || window.Id != excludingWindowId.Value),
            cancellationToken);

    public async Task AddAsync(BerthWindow window, CancellationToken cancellationToken)
    {
        await database.BerthWindows.AddAsync(window, cancellationToken);
    }

    public async Task<BerthWindowResponse?> GetDetailsByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var window = await DetailsQuery()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return window is null ? null : ToResponse(window);
    }

    public async Task<BerthWindowResponse?> GetActiveDetailsByPublicCodeAsync(
        string publicCode,
        CancellationToken cancellationToken)
    {
        var window = await DetailsQuery()
            .SingleOrDefaultAsync(
                item => item.PortCall.PublicCode == publicCode
                    && (item.Status == BerthWindowStatus.Requested
                        || item.Status == BerthWindowStatus.Confirmed),
                cancellationToken);
        return window is null ? null : ToResponse(window);
    }

    public async Task<PagedResult<BerthWindowResponse>> ListAsync(
        ListBerthWindowsQuery query,
        CancellationToken cancellationToken)
    {
        var windows = DetailsQuery();

        if (query.PortId.HasValue)
        {
            windows = windows.Where(window => window.PortCall.PortId == query.PortId.Value);
        }
        if (query.BerthId.HasValue)
        {
            windows = windows.Where(window => window.BerthId == query.BerthId.Value);
        }
        if (query.Status.HasValue)
        {
            windows = windows.Where(window => window.Status == query.Status.Value);
        }
        if (query.FromUtc.HasValue)
        {
            windows = windows.Where(window => window.EndsAtUtc > query.FromUtc.Value);
        }
        if (query.ToUtc.HasValue)
        {
            windows = windows.Where(window => window.StartsAtUtc < query.ToUtc.Value);
        }

        var totalItems = await windows.CountAsync(cancellationToken);
        var page = await windows
            .OrderBy(window => window.StartsAtUtc)
            .ThenBy(window => window.Berth.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BerthWindowResponse>(
            page.Select(ToResponse).ToArray(),
            query.Page,
            query.PageSize,
            totalItems);
    }

    private IQueryable<BerthWindow> DetailsQuery() => database.BerthWindows
        .ApplyOrganizationScope(dataScope)
        .AsNoTracking()
        .Include(window => window.PortCall)
            .ThenInclude(portCall => portCall.Vessel)
        .Include(window => window.PortCall)
            .ThenInclude(portCall => portCall.Port)
        .Include(window => window.Berth)
            .ThenInclude(berth => berth.Terminal)
        .Include(window => window.Revisions);

    private static BerthWindowResponse ToResponse(BerthWindow window) => new(
        window.Id,
        window.PortCallId,
        window.PortCall.PublicCode,
        window.PortCall.VesselId,
        window.PortCall.Vessel.Name,
        window.PortCall.PortId,
        window.PortCall.Port.Name,
        window.Berth.TerminalId,
        window.Berth.Terminal.Name,
        window.BerthId,
        window.Berth.Code,
        window.Berth.Name,
        window.StartsAtUtc,
        window.EndsAtUtc,
        window.Status,
        window.RequestedBy,
        window.LastChangedBy,
        window.LastChangeReason,
        window.Version,
        window.CreatedAtUtc,
        window.UpdatedAtUtc,
        window.Revisions
            .OrderBy(revision => revision.ChangedAtUtc)
            .Select(revision => new BerthWindowRevisionResponse(
                revision.PreviousBerthId,
                revision.NewBerthId,
                revision.PreviousStartsAtUtc,
                revision.PreviousEndsAtUtc,
                revision.NewStartsAtUtc,
                revision.NewEndsAtUtc,
                revision.ChangedBy,
                revision.Reason,
                revision.ChangedAtUtc))
            .ToArray());
}
