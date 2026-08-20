using Microsoft.EntityFrameworkCore;
using PortManagement.Application.ReferenceData;

namespace PortManagement.Infrastructure.Persistence.Repositories;

internal sealed class PortStructureRepository(PortManagementDbContext database) : IPortStructureRepository
{
    public async Task<IReadOnlyCollection<PortReferenceResponse>> ListActiveAsync(
        CancellationToken cancellationToken)
    {
        var ports = await database.Ports
            .AsNoTracking()
            .Where(port => port.IsActive)
            .OrderBy(port => port.Name)
            .ToListAsync(cancellationToken);

        var portIds = ports.Select(port => port.Id).ToArray();
        var terminals = await database.Terminals
            .AsNoTracking()
            .Where(terminal => terminal.IsActive && portIds.Contains(terminal.PortId))
            .OrderBy(terminal => terminal.Name)
            .ToListAsync(cancellationToken);

        var terminalIds = terminals.Select(terminal => terminal.Id).ToArray();
        var berths = await database.Berths
            .AsNoTracking()
            .Where(berth => terminalIds.Contains(berth.TerminalId))
            .OrderBy(berth => berth.Code)
            .ToListAsync(cancellationToken);

        return ports
            .Select(port => new PortReferenceResponse(
                port.Id,
                port.Name,
                port.UnLocode,
                port.CountryCode,
                port.TimeZoneId,
                terminals
                    .Where(terminal => terminal.PortId == port.Id)
                    .Select(terminal => new TerminalReferenceResponse(
                        terminal.Id,
                        terminal.Code,
                        terminal.Name,
                        terminal.TimeZoneId,
                        berths
                            .Where(berth => berth.TerminalId == terminal.Id)
                            .Select(berth => new BerthReferenceResponse(
                                berth.Id,
                                berth.Code,
                                berth.Name,
                                berth.UsefulLengthMeters,
                                berth.MaximumBeamMeters,
                                berth.MaximumDraftMeters,
                                berth.SupportedVesselTypes,
                                berth.Status))
                            .ToArray()))
                    .ToArray()))
            .ToArray();
    }
}
