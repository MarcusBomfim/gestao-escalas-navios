using Microsoft.EntityFrameworkCore;
using PortManagement.Application.Administration;
using PortManagement.Application.Common;
using PortManagement.Domain.Common;
using PortManagement.Domain.Organizations;
using PortManagement.Domain.Planning;
using PortManagement.Domain.Ports;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class MasterDataRepository(PortManagementDbContext database) : IMasterDataRepository
{
    public async Task<PagedResult<OrganizationAdminResponse>> ListOrganizationsAsync(
        ListOrganizationsQuery query,
        CancellationToken cancellationToken)
    {
        var organizations = database.Organizations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            organizations = organizations.Where(organization =>
                EF.Functions.ILike(organization.Name, pattern)
                || EF.Functions.ILike(organization.RegistrationNumber, pattern));
        }

        if (query.Type is OrganizationType type)
        {
            organizations = organizations.Where(organization => organization.Type == type);
        }

        if (query.IsActive is bool isActive)
        {
            organizations = organizations.Where(organization => organization.IsActive == isActive);
        }

        var totalItems = await organizations.CountAsync(cancellationToken);
        var items = await organizations
            .OrderBy(organization => organization.Name)
            .ThenBy(organization => organization.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(organization => new OrganizationAdminResponse(
                organization.Id,
                organization.Name,
                organization.RegistrationNumber,
                organization.Type,
                organization.IsActive,
                organization.CreatedAtUtc,
                organization.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<OrganizationAdminResponse>(
            items,
            query.Page,
            query.PageSize,
            totalItems);
    }

    public Task<bool> OrganizationRegistrationExistsAsync(
        string registrationNumber,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        database.Organizations.AnyAsync(
            organization => organization.RegistrationNumber == registrationNumber
                && (!excludingId.HasValue || organization.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> OrganizationHasActiveUsersAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        database.Users.AnyAsync(
            user => user.OrganizationId == organizationId && user.IsActive,
            cancellationToken);

    public async Task AddOrganizationAsync(
        Organization organization,
        CancellationToken cancellationToken) =>
        await database.Organizations.AddAsync(organization, cancellationToken);

    public Task<Organization?> FindOrganizationAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        database.Organizations.SingleOrDefaultAsync(
            organization => organization.Id == id,
            cancellationToken);

    public async Task<IReadOnlyCollection<PortAdminResponse>> ListPortStructureAsync(
        CancellationToken cancellationToken)
    {
        var ports = await database.Ports
            .AsNoTracking()
            .OrderByDescending(port => port.IsActive)
            .ThenBy(port => port.Name)
            .ToArrayAsync(cancellationToken);
        var portIds = ports.Select(port => port.Id).ToArray();
        var terminals = await database.Terminals
            .AsNoTracking()
            .Where(terminal => portIds.Contains(terminal.PortId))
            .OrderByDescending(terminal => terminal.IsActive)
            .ThenBy(terminal => terminal.Name)
            .ToArrayAsync(cancellationToken);
        var terminalIds = terminals.Select(terminal => terminal.Id).ToArray();
        var berths = await database.Berths
            .AsNoTracking()
            .Where(berth => terminalIds.Contains(berth.TerminalId))
            .OrderBy(berth => berth.Code)
            .ToArrayAsync(cancellationToken);

        return ports.Select(port => port.ToAdminResponse(
            terminals
                .Where(terminal => terminal.PortId == port.Id)
                .Select(terminal => terminal.ToAdminResponse(
                    berths
                        .Where(berth => berth.TerminalId == terminal.Id)
                        .Select(berth => berth.ToAdminResponse())
                        .ToArray()))
                .ToArray()))
            .ToArray();
    }

    public Task<bool> PortUnLocodeExistsAsync(
        string unLocode,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        database.Ports.AnyAsync(
            port => port.UnLocode == unLocode
                && (!excludingId.HasValue || port.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> PortHasActiveTerminalsAsync(
        Guid portId,
        CancellationToken cancellationToken) =>
        database.Terminals.AnyAsync(
            terminal => terminal.PortId == portId && terminal.IsActive,
            cancellationToken);

    public async Task AddPortAsync(Port port, CancellationToken cancellationToken) =>
        await database.Ports.AddAsync(port, cancellationToken);

    public Task<Port?> FindPortAsync(Guid id, CancellationToken cancellationToken) =>
        database.Ports.SingleOrDefaultAsync(port => port.Id == id, cancellationToken);

    public Task<bool> TerminalCodeExistsAsync(
        Guid portId,
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        database.Terminals.AnyAsync(
            terminal => terminal.PortId == portId
                && terminal.Code == code
                && (!excludingId.HasValue || terminal.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> TerminalHasAvailableBerthsAsync(
        Guid terminalId,
        CancellationToken cancellationToken) =>
        database.Berths.AnyAsync(
            berth => berth.TerminalId == terminalId
                && berth.Status == BerthStatus.Available,
            cancellationToken);

    public async Task AddTerminalAsync(
        Terminal terminal,
        CancellationToken cancellationToken) =>
        await database.Terminals.AddAsync(terminal, cancellationToken);

    public Task<Terminal?> FindTerminalAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        database.Terminals.SingleOrDefaultAsync(terminal => terminal.Id == id, cancellationToken);

    public Task<bool> BerthCodeExistsAsync(
        Guid terminalId,
        string code,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        database.Berths.AnyAsync(
            berth => berth.TerminalId == terminalId
                && berth.Code == code
                && (!excludingId.HasValue || berth.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> BerthHasOpenWindowsAsync(
        Guid berthId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken) =>
        database.BerthWindows.AnyAsync(
            window => window.BerthId == berthId
                && window.EndsAtUtc > fromUtc
                && (window.Status == BerthWindowStatus.Requested
                    || window.Status == BerthWindowStatus.Confirmed),
            cancellationToken);

    public async Task AddBerthAsync(Berth berth, CancellationToken cancellationToken) =>
        await database.Berths.AddAsync(berth, cancellationToken);

    public Task<Berth?> FindBerthAsync(Guid id, CancellationToken cancellationToken) =>
        database.Berths.SingleOrDefaultAsync(berth => berth.Id == id, cancellationToken);

    public void UseExpectedUpdatedAt(
        AuditableEntity entity,
        DateTimeOffset expectedUpdatedAtUtc)
    {
        database.Entry(entity).Property(nameof(AuditableEntity.UpdatedAtUtc)).OriginalValue =
            expectedUpdatedAtUtc.ToUniversalTime();
    }
}
